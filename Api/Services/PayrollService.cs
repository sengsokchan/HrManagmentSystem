using HrManagementSystem.Domain;

namespace HrManagementSystem.Application.Services;

public sealed class PayrollService(IHrRepository repository) : IPayrollService
{
    private const decimal OvertimeMultiplier = 1.5m;
    private const decimal StandardMonthlyHours = 176m; // 22 days * 8 hours
    private const decimal AllowanceRate = 0.10m;
    private const decimal TaxRate = 0.05m;

    public PagedResult<PayrollView> GetPayroll(UserContext user, ListQuery query)
    {
        IEnumerable<PayrollRun> source = repository.PayrollRuns;
        if (!AuthorizationRules.Can(user, "payroll.read"))
        {
            if (user.EmployeeId is null) throw new UnauthorizedAccessException();
            source = source.Where(p => p.EmployeeId == user.EmployeeId);
        }

        var views = source.Select(payroll => payroll.ToPayrollView(repository));
        var search = query.Search?.Trim() ?? string.Empty;
        var status = query.Status?.Trim() ?? string.Empty;

        return Paging.Apply(views, query, item =>
        {
            if (!string.IsNullOrWhiteSpace(status) &&
                !string.Equals(item.Status.ToString(), status, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(search)) return true;
            return item.EmployeeName.Contains(search, StringComparison.OrdinalIgnoreCase);
        });
    }

    public PayrollGenerateResult GeneratePayroll(PayrollGenerateRequest request, UserContext user)
    {
        if (!AuthorizationRules.Can(user, "payroll.write") && !AuthorizationRules.Can(user, "employees.write"))
        {
            throw new UnauthorizedAccessException();
        }

        if (request.PeriodEnd < request.PeriodStart)
        {
            throw new ArgumentException("Period end must be on or after period start.");
        }

        var created = new List<PayrollView>();
        var skipped = 0;

        var activeEmployees = repository.Employees
            .Where(e => e.Status == EmployeeStatus.Active)
            .OrderBy(e => e.Id)
            .ToList();

        foreach (var employee in activeEmployees)
        {
            var exists = repository.PayrollRuns.Any(p =>
                p.EmployeeId == employee.Id &&
                p.PeriodStart == request.PeriodStart &&
                p.PeriodEnd == request.PeriodEnd);

            if (exists)
            {
                skipped++;
                continue;
            }

            var overtimeMinutes = repository.Attendance
                .Where(a => a.EmployeeId == employee.Id &&
                            a.WorkDate >= request.PeriodStart &&
                            a.WorkDate <= request.PeriodEnd)
                .Sum(a => a.OvertimeMinutes);

            var basic = employee.BasicSalary > 0 ? employee.BasicSalary : 500m;
            var hourly = basic / StandardMonthlyHours;
            var overtimePay = Math.Round(hourly * OvertimeMultiplier * (overtimeMinutes / 60m), 2);
            var allowance = Math.Round(basic * AllowanceRate, 2);
            var bonus = 0m;
            var taxable = basic + allowance + overtimePay + bonus;
            var tax = Math.Round(taxable * TaxRate, 2);
            var deduction = 0m;

            var run = repository.CreatePayrollRun(new PayrollRun(
                0,
                employee.Id,
                request.PeriodStart,
                request.PeriodEnd,
                basic,
                allowance,
                bonus,
                tax,
                deduction,
                overtimePay,
                PayrollStatus.Draft));

            created.Add(run.ToPayrollView(repository));
        }

        repository.AddAudit(
            user.UserId,
            "Generated payroll",
            "Payroll",
            $"{request.PeriodStart:yyyy-MM-dd}_{request.PeriodEnd:yyyy-MM-dd}",
            $"Created {created.Count}, skipped {skipped}");

        return new PayrollGenerateResult(created.Count, skipped, created);
    }

    public PayrollView? UpdatePayrollStatus(int id, PayrollStatusUpdateRequest request, UserContext user)
    {
        if (!AuthorizationRules.Can(user, "payroll.write") && !AuthorizationRules.Can(user, "employees.write"))
        {
            throw new UnauthorizedAccessException();
        }

        var payroll = repository.PayrollRuns.FirstOrDefault(p => p.Id == id);
        if (payroll is null) return null;

        if (!Enum.TryParse<PayrollStatus>(request.Status.Trim(), true, out var next))
        {
            throw new ArgumentException("Status must be Draft, Approved, or Paid.");
        }

        var allowed = (payroll.Status, next) switch
        {
            (PayrollStatus.Draft, PayrollStatus.Approved) => true,
            (PayrollStatus.Approved, PayrollStatus.Paid) => true,
            (PayrollStatus.Draft, PayrollStatus.Paid) => true, // allow direct pay for small teams
            _ => false
        };

        if (!allowed)
        {
            throw new InvalidOperationException($"Cannot change payroll from {payroll.Status} to {next}.");
        }

        var updated = repository.UpdatePayrollStatus(id, next);
        if (updated is null) return null;

        repository.AddAudit(user.UserId, next.ToString(), "Payroll", id.ToString(), $"{updated.EmployeeId} {updated.PeriodStart:yyyy-MM}");
        return updated.ToPayrollView(repository);
    }
}
