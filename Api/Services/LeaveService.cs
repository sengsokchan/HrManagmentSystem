using HrManagementSystem.Domain;

namespace HrManagementSystem.Application.Services;

public sealed class LeaveService(IHrRepository repository) : ILeaveService
{
    public PagedResult<LeaveView> GetLeaveRequests(UserContext user, ListQuery query)
    {
        var requests = AuthorizationRules.Can(user, "leave.read") || AuthorizationRules.Can(user, "employees.read")
            ? repository.LeaveRequests
            : repository.LeaveRequests.Where(l => l.EmployeeId == user.EmployeeId).ToList();

        var views = requests.Select(request => request.ToLeaveView(repository));
        var search = query.Search?.Trim() ?? string.Empty;
        var status = query.Status?.Trim() ?? string.Empty;

        return Paging.Apply(views, query, item =>
        {
            if (query.From is not null && item.StartDate < query.From.Value) return false;
            if (query.To is not null && item.EndDate > query.To.Value) return false;
            if (!string.IsNullOrWhiteSpace(status) &&
                !string.Equals(item.Status.ToString(), status, StringComparison.OrdinalIgnoreCase) &&
                !(status.Equals("Pending", StringComparison.OrdinalIgnoreCase) &&
                  (item.Status == LeaveStatus.Pending || item.Status == LeaveStatus.ManagerApproved)))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(search)) return true;
            var haystack = $"{item.EmployeeName} {item.LeaveType} {item.Reason}";
            return haystack.Contains(search, StringComparison.OrdinalIgnoreCase);
        });
    }

    public LeaveView CreateLeaveRequest(LeaveCreateRequest request, UserContext user)
    {
        var employeeId = request.EmployeeId ?? user.EmployeeId;
        if (employeeId is null) throw new InvalidOperationException("No employee is linked to this leave request.");
        if (request.EmployeeId is not null && request.EmployeeId != user.EmployeeId && !AuthorizationRules.Can(user, "leave.write"))
        {
            throw new UnauthorizedAccessException();
        }

        var days = LeaveCalculator.CalculateDays(request.StartDate, request.EndDate, request.IsHalfDay);
        EnsureBalanceAvailable(employeeId.Value, request.LeaveType, request.StartDate.Year, days, excludeRequestId: null);

        var leave = repository.CreateLeaveRequest(employeeId.Value, request);
        repository.AddAudit(user.UserId, "Created", "LeaveRequest", leave.Id.ToString(), $"{leave.LeaveType} ({days} day(s))");
        return leave.ToLeaveView(repository);
    }

    public LeaveView? DecideLeave(int id, LeaveDecisionRequest request, UserContext user)
    {
        var leave = repository.LeaveRequests.FirstOrDefault(l => l.Id == id);
        if (leave is null) return null;

        var decision = request.Decision.Trim().Equals("approve", StringComparison.OrdinalIgnoreCase)
            ? LeaveStatus.Approved
            : LeaveStatus.Rejected;

        if (user.Role == "Manager" && leave.Status == LeaveStatus.Pending)
        {
            decision = request.Decision.Trim().Equals("approve", StringComparison.OrdinalIgnoreCase)
                ? LeaveStatus.ManagerApproved
                : LeaveStatus.Rejected;
        }
        else if (!AuthorizationRules.Can(user, "leave.approve.hr"))
        {
            throw new UnauthorizedAccessException();
        }

        if (decision == LeaveStatus.Approved)
        {
            var days = LeaveCalculator.CalculateDays(leave.StartDate, leave.EndDate, leave.IsHalfDay);
            EnsureBalanceAvailable(leave.EmployeeId, leave.LeaveType, leave.StartDate.Year, days, excludeRequestId: leave.Id);
        }

        var updated = (repository as ILeaveDecisionRepository)?.UpdateLeaveDecision(id, decision, request.Comment, user.Role);
        if (updated is null) throw new InvalidOperationException("Leave decision storage is not available.");
        repository.AddAudit(user.UserId, decision.ToString(), "LeaveRequest", id.ToString(), request.Comment ?? string.Empty);
        return updated.ToLeaveView(repository);
    }

    public IReadOnlyList<LeaveBalanceView> GetLeaveBalances(UserContext user, int? employeeId, int? year)
    {
        var targetEmployeeId = employeeId ?? user.EmployeeId
            ?? throw new InvalidOperationException("Employee is required to view leave balances.");

        if (targetEmployeeId != user.EmployeeId &&
            !AuthorizationRules.Can(user, "leave.read") &&
            !AuthorizationRules.Can(user, "leave.write") &&
            !AuthorizationRules.Can(user, "employees.read"))
        {
            throw new UnauthorizedAccessException();
        }

        var targetYear = year ?? DateTime.UtcNow.Year;
        repository.EnsureDefaultLeaveBalances(targetEmployeeId, targetYear);
        return BuildBalanceViews(targetEmployeeId, targetYear);
    }

    public LeaveBalanceView UpsertLeaveBalance(LeaveBalanceUpsertRequest request, UserContext user)
    {
        if (!AuthorizationRules.Can(user, "leave.write") && !AuthorizationRules.Can(user, "employees.write"))
        {
            throw new UnauthorizedAccessException();
        }

        var leaveType = request.LeaveType.Trim();
        if (!LeaveCalculator.TracksBalance(leaveType))
        {
            throw new ArgumentException("This leave type does not use a balance entitlement.");
        }

        if (request.Year < 2000 || request.Year > 2100)
        {
            throw new ArgumentException("Year is invalid.");
        }

        if (request.EntitledDays < 0 || request.EntitledDays > 366)
        {
            throw new ArgumentException("Entitled days must be between 0 and 366.");
        }

        if (repository.Employees.All(e => e.Id != request.EmployeeId))
        {
            throw new KeyNotFoundException("Employee not found.");
        }

        repository.UpsertLeaveBalance(request.EmployeeId, leaveType, request.Year, request.EntitledDays);
        repository.AddAudit(
            user.UserId,
            "Updated leave balance",
            "LeaveBalance",
            $"{request.EmployeeId}:{leaveType}:{request.Year}",
            $"{request.EntitledDays} entitled day(s)");

        return BuildBalanceViews(request.EmployeeId, request.Year)
            .First(b => b.LeaveType.Equals(leaveType, StringComparison.OrdinalIgnoreCase));
    }

    private IReadOnlyList<LeaveBalanceView> BuildBalanceViews(int employeeId, int year)
    {
        var employee = repository.Employees.FirstOrDefault(e => e.Id == employeeId)
            ?? throw new KeyNotFoundException("Employee not found.");

        var rows = repository.GetLeaveBalanceRows(employeeId, year);
        var usage = repository.LeaveRequests
            .Where(l => l.EmployeeId == employeeId && l.StartDate.Year == year)
            .Where(l => LeaveCalculator.TracksBalance(l.LeaveType))
            .GroupBy(l => l.LeaveType, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => (
                    Used: g.Where(l => l.Status == LeaveStatus.Approved)
                        .Sum(l => LeaveCalculator.DisplayDays(l.StartDate, l.EndDate, l.IsHalfDay)),
                    Pending: g.Where(l => l.Status is LeaveStatus.Pending or LeaveStatus.ManagerApproved)
                        .Sum(l => LeaveCalculator.DisplayDays(l.StartDate, l.EndDate, l.IsHalfDay))),
                StringComparer.OrdinalIgnoreCase);

        return rows
            .OrderBy(r => r.LeaveType)
            .Select(row =>
            {
                usage.TryGetValue(row.LeaveType, out var counts);
                var used = counts.Used;
                var pending = counts.Pending;
                var remaining = row.EntitledDays - used - pending;
                return new LeaveBalanceView(
                    employeeId,
                    employee.FullName,
                    row.LeaveType,
                    year,
                    row.EntitledDays,
                    used,
                    pending,
                    remaining);
            })
            .ToList();
    }

    private void EnsureBalanceAvailable(int employeeId, string leaveType, int year, decimal requestedDays, int? excludeRequestId)
    {
        if (!LeaveCalculator.TracksBalance(leaveType)) return;

        repository.EnsureDefaultLeaveBalances(employeeId, year);
        var balance = BuildBalanceViews(employeeId, year)
            .FirstOrDefault(b => b.LeaveType.Equals(leaveType, StringComparison.OrdinalIgnoreCase));

        if (balance is null)
        {
            throw new InvalidOperationException($"No leave balance is configured for {leaveType}.");
        }

        var available = balance.RemainingDays;
        if (excludeRequestId is not null)
        {
            var existing = repository.LeaveRequests.FirstOrDefault(l => l.Id == excludeRequestId);
            if (existing is not null &&
                (existing.Status == LeaveStatus.Pending || existing.Status == LeaveStatus.ManagerApproved) &&
                existing.LeaveType.Equals(leaveType, StringComparison.OrdinalIgnoreCase) &&
                existing.StartDate.Year == year)
            {
                available += LeaveCalculator.DisplayDays(existing.StartDate, existing.EndDate, existing.IsHalfDay);
            }
        }

        if (requestedDays > available)
        {
            throw new InvalidOperationException(
                $"Not enough {leaveType} balance. Requested {requestedDays} day(s), remaining {available} day(s).");
        }
    }
}

public interface ILeaveDecisionRepository
{
    LeaveRequest? UpdateLeaveDecision(int id, LeaveStatus status, string? comment, string actorRole);
}
