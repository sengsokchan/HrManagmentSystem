using HrManagementSystem.Domain;

namespace HrManagementSystem.Application.Services;

public sealed class AttendanceService(IHrRepository repository) : IAttendanceService
{
    public PagedResult<AttendanceView> GetAttendance(UserContext user, ListQuery query)
    {
        IEnumerable<AttendanceRecord> records =
            AuthorizationRules.Can(user, "attendance.read") || AuthorizationRules.Can(user, "employees.read")
                ? repository.Attendance
                : repository.Attendance.Where(record => record.EmployeeId == user.EmployeeId);

        var views = records.Select(record => record.ToAttendanceView(repository));
        var search = query.Search?.Trim() ?? string.Empty;
        var status = query.Status?.Trim() ?? string.Empty;

        return Paging.Apply(views, query, item =>
        {
            if (query.From is not null && item.WorkDate < query.From.Value) return false;
            if (query.To is not null && item.WorkDate > query.To.Value) return false;
            if (!string.IsNullOrWhiteSpace(status) &&
                !string.Equals(item.Status.ToString(), status, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(search)) return true;
            return item.EmployeeName.Contains(search, StringComparison.OrdinalIgnoreCase);
        });
    }

    public AttendanceView CheckIn(AttendanceCheckRequest request, UserContext user)
    {
        var employeeId = ResolveEmployeeId(request, user);
        var attendance = repository.CheckIn(employeeId, request);
        repository.AddAudit(user.UserId, "Checked in", "Attendance", attendance.Id.ToString(), attendance.WorkMode);
        return attendance.ToAttendanceView(repository);
    }

    public AttendanceView? CheckOut(AttendanceCheckRequest request, UserContext user)
    {
        var employeeId = ResolveEmployeeId(request, user);
        var attendance = repository.CheckOut(employeeId);
        if (attendance is not null) repository.AddAudit(user.UserId, "Checked out", "Attendance", attendance.Id.ToString(), attendance.WorkMode);
        return attendance?.ToAttendanceView(repository);
    }

    private static int ResolveEmployeeId(AttendanceCheckRequest request, UserContext user)
    {
        if (request.EmployeeId is not null && AuthorizationRules.Can(user, "attendance.write")) return request.EmployeeId.Value;
        if (user.EmployeeId is not null) return user.EmployeeId.Value;
        throw new InvalidOperationException("No employee is linked to this account.");
    }
}
