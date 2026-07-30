using HrManagementSystem.Domain;

namespace HrManagementSystem.Application.Services;

public sealed class DashboardService(IHrRepository repository, IBusinessClock clock) : IDashboardService
{
    public DashboardView GetDashboard()
    {
        var today = clock.Today;
        var attendanceToday = repository.Attendance.Where(a => a.WorkDate == today).ToList();
        var leaveToday = repository.LeaveRequests
            .Where(l =>
                (l.Status is LeaveStatus.Approved or LeaveStatus.ManagerApproved) &&
                l.StartDate <= today &&
                l.EndDate >= today)
            .ToList();

        return new DashboardView(
            repository.Employees.Count(e => e.Status == EmployeeStatus.Active),
            attendanceToday.Count,
            attendanceToday.Count(a => a.Status == AttendanceStatus.Late),
            leaveToday.Select(l => l.EmployeeId).Distinct().Count(),
            repository.LeaveRequests.Count(l =>
                l.Status is LeaveStatus.Pending or LeaveStatus.ManagerApproved),
            repository.PayrollRuns
                .Where(p => p.Status is PayrollStatus.Approved or PayrollStatus.Draft)
                .Sum(p => p.NetSalary),
            repository.AuditLogs.OrderByDescending(a => a.CreatedAt).Take(20).ToList());
    }
}
