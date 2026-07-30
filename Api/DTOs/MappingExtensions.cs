using HrManagementSystem.Domain;

namespace HrManagementSystem.Application;

public static class MappingExtensions
{
    public static EmployeeView ToEmployeeView(this Employee employee, IHrRepository repository) =>
        new(
            employee.Id,
            employee.EmployeeCode,
            employee.FullName,
            employee.Gender,
            employee.DateOfBirth,
            employee.Email,
            employee.Phone,
            employee.DepartmentId,
            repository.Departments.FirstOrDefault(d => d.Id == employee.DepartmentId)?.Name ?? string.Empty,
            employee.PositionId,
            repository.Positions.FirstOrDefault(p => p.Id == employee.PositionId)?.Title ?? string.Empty,
            employee.BranchId,
            repository.Branches.FirstOrDefault(b => b.Id == employee.BranchId)?.Name ?? string.Empty,
            employee.ManagerId,
            repository.Employees.FirstOrDefault(e => e.Id == employee.ManagerId)?.FullName,
            employee.ContractType,
            employee.JoinDate,
            employee.ResignDate,
            employee.Status,
            employee.EmergencyContact,
            employee.EducationHistory,
            employee.WorkExperience,
            employee.BasicSalary);

    public static AttendanceView ToAttendanceView(this AttendanceRecord record, IHrRepository repository) =>
        new(
            record.Id,
            record.EmployeeId,
            repository.Employees.FirstOrDefault(e => e.Id == record.EmployeeId)?.FullName ?? string.Empty,
            record.WorkDate,
            record.CheckIn,
            record.CheckOut,
            record.Status,
            record.Latitude,
            record.Longitude,
            record.WorkMode,
            record.LateMinutes,
            record.OvertimeMinutes);

    public static LeaveView ToLeaveView(this LeaveRequest request, IHrRepository repository) =>
        new(
            request.Id,
            request.EmployeeId,
            repository.Employees.FirstOrDefault(e => e.Id == request.EmployeeId)?.FullName ?? string.Empty,
            request.LeaveType,
            request.StartDate,
            request.EndDate,
            request.IsHalfDay,
            LeaveCalculator.DisplayDays(request.StartDate, request.EndDate, request.IsHalfDay),
            request.Reason,
            request.AttachmentUrl,
            request.Status,
            request.ManagerComment,
            request.HrComment,
            request.CreatedAt,
            request.UpdatedAt);

    public static PayrollView ToPayrollView(this PayrollRun payroll, IHrRepository repository) =>
        new(
            payroll.Id,
            payroll.EmployeeId,
            repository.Employees.FirstOrDefault(e => e.Id == payroll.EmployeeId)?.FullName ?? string.Empty,
            payroll.PeriodStart,
            payroll.PeriodEnd,
            payroll.BasicSalary,
            payroll.Allowance,
            payroll.Bonus,
            payroll.Tax,
            payroll.Deduction,
            payroll.OvertimePay,
            payroll.NetSalary,
            payroll.Status);
}
