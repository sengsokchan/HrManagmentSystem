namespace HrManagementSystem.Domain;

public sealed record Department(int Id, string Name);
public sealed record Position(int Id, int DepartmentId, string Title);
public sealed record Branch(int Id, string Name, string Address, decimal Latitude, decimal Longitude);
public sealed record Role(int Id, string Name);
public sealed record UserAccount(
    int Id,
    int? EmployeeId,
    int RoleId,
    string Email,
    string PasswordHash,
    bool IsActive,
    bool MustChangePassword);

public sealed record Employee(
    int Id,
    string EmployeeCode,
    string FullName,
    string Gender,
    DateOnly DateOfBirth,
    string Email,
    string Phone,
    int DepartmentId,
    int PositionId,
    int BranchId,
    int? ManagerId,
    string ContractType,
    DateOnly JoinDate,
    DateOnly? ResignDate,
    EmployeeStatus Status,
    string EmergencyContact,
    string EducationHistory,
    string WorkExperience,
    decimal BasicSalary,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record AttendanceRecord(
    int Id,
    int EmployeeId,
    DateOnly WorkDate,
    DateTime? CheckIn,
    DateTime? CheckOut,
    AttendanceStatus Status,
    decimal? Latitude,
    decimal? Longitude,
    string WorkMode,
    int LateMinutes,
    int OvertimeMinutes);

public sealed record LeaveRequest(
    int Id,
    int EmployeeId,
    string LeaveType,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsHalfDay,
    string Reason,
    string? AttachmentUrl,
    LeaveStatus Status,
    string? ManagerComment,
    string? HrComment,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record LeaveBalance(
    int Id,
    int EmployeeId,
    string LeaveType,
    int Year,
    decimal EntitledDays);

public sealed record PayrollRun(
    int Id,
    int EmployeeId,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    decimal BasicSalary,
    decimal Allowance,
    decimal Bonus,
    decimal Tax,
    decimal Deduction,
    decimal OvertimePay,
    PayrollStatus Status)
{
    public decimal NetSalary => BasicSalary + Allowance + Bonus + OvertimePay - Tax - Deduction;
}

public sealed record AuditLog(
    long Id,
    int UserId,
    string Action,
    string EntityName,
    string EntityId,
    string Details,
    DateTime CreatedAt);
