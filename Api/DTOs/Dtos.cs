using HrManagementSystem.Domain;

namespace HrManagementSystem.Application;

public sealed record LoginRequest(string Email, string Password);
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public sealed record ForgotPasswordRequest(string Email, string EmployeeCode);
public sealed record PasswordResetResult(string FullName, string LoginEmail, string TemporaryPassword);
public sealed record UserContext(int UserId, int? EmployeeId, string Email, string Role, string[] Permissions);
public sealed record TokenPayload(int UserId, int? EmployeeId, string Email, string Role, string[] Permissions, long ExpiresAt, string Issuer, string Audience);

public sealed record LoginResponse(string Token, SignedInUser User, bool MustChangePassword);
public sealed record LoginResult(LoginResultStatus Status, LoginResponse? Response, string Message, DateTime? RetryAt);
public sealed record SignedInUser(int Id, string Email, string Role, int? EmployeeId, string? EmployeeName, IReadOnlyCollection<string> Permissions, bool MustChangePassword);
public sealed record CurrentUserView(int UserId, string Email, string Role, int? EmployeeId, string? EmployeeName, IReadOnlyCollection<string> Permissions, bool MustChangePassword);
public sealed record EmployeeCreateResult(EmployeeView Employee, string LoginEmail, string TemporaryPassword);
public sealed record LoginSecurityState(string Email, int FailedAccessCount, int LockoutCycles, DateTime? LockoutEndAt, bool RequiresAdminReset);

public sealed record DashboardView(
    int TotalEmployees,
    int TodayAttendance,
    int LateEmployees,
    int EmployeesOnLeave,
    int PendingLeave,
    decimal PayrollSummary,
    IEnumerable<AuditLog> RecentActivity);

public sealed record EmployeeWriteRequest(
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
    decimal BasicSalary);

public sealed record AttendanceCheckRequest(int? EmployeeId, decimal? Latitude, decimal? Longitude, string WorkMode);
public sealed record LeaveCreateRequest(int? EmployeeId, string LeaveType, DateOnly StartDate, DateOnly EndDate, bool IsHalfDay, string Reason, string? AttachmentUrl);
public sealed record LeaveDecisionRequest(string Decision, string? Comment);
public sealed record LeaveBalanceUpsertRequest(int EmployeeId, string LeaveType, int Year, decimal EntitledDays);
public sealed record PayrollGenerateRequest(DateOnly PeriodStart, DateOnly PeriodEnd);
public sealed record PayrollGenerateResult(int Created, int Skipped, IReadOnlyList<PayrollView> Items);
public sealed record PayrollStatusUpdateRequest(string Status);

public sealed record EmployeeView(
    int Id,
    string EmployeeCode,
    string FullName,
    string Gender,
    DateOnly DateOfBirth,
    string Email,
    string Phone,
    int DepartmentId,
    string Department,
    int PositionId,
    string Position,
    int BranchId,
    string Branch,
    int? ManagerId,
    string? Manager,
    string ContractType,
    DateOnly JoinDate,
    DateOnly? ResignDate,
    EmployeeStatus Status,
    string EmergencyContact,
    string EducationHistory,
    string WorkExperience,
    decimal BasicSalary);

public sealed record AttendanceView(
    int Id,
    int EmployeeId,
    string EmployeeName,
    DateOnly WorkDate,
    DateTime? CheckIn,
    DateTime? CheckOut,
    AttendanceStatus Status,
    decimal? Latitude,
    decimal? Longitude,
    string WorkMode,
    int LateMinutes,
    int OvertimeMinutes);

public sealed record LeaveView(
    int Id,
    int EmployeeId,
    string EmployeeName,
    string LeaveType,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsHalfDay,
    decimal Days,
    string Reason,
    string? AttachmentUrl,
    LeaveStatus Status,
    string? ManagerComment,
    string? HrComment,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record LeaveBalanceView(
    int EmployeeId,
    string EmployeeName,
    string LeaveType,
    int Year,
    decimal EntitledDays,
    decimal UsedDays,
    decimal PendingDays,
    decimal RemainingDays);

public sealed record RoleView(int Id, string Name, IReadOnlyCollection<string> Permissions);

public sealed record PayrollView(
    int Id,
    int EmployeeId,
    string EmployeeName,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    decimal BasicSalary,
    decimal Allowance,
    decimal Bonus,
    decimal Tax,
    decimal Deduction,
    decimal OvertimePay,
    decimal NetSalary,
    PayrollStatus Status);

public sealed record PagedResult<T>(IEnumerable<T> Items, int Total, int Page, int PageSize);

public sealed record ListQuery(
    int Page = 1,
    int PageSize = 10,
    string? Search = null,
    string? Status = null,
    DateOnly? From = null,
    DateOnly? To = null);

public enum LoginResultStatus
{
    Success,
    InvalidCredentials,
    TemporarilyLocked,
    AdminLocked
}

public static class Paging
{
    public static PagedResult<T> Apply<T>(IEnumerable<T> source, ListQuery query, Func<T, bool>? predicate = null)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 10 : Math.Min(query.PageSize, 2000);
        var filtered = predicate is null ? source : source.Where(predicate);
        var materialized = filtered.ToList();
        var items = materialized.Skip((page - 1) * pageSize).Take(pageSize);
        return new PagedResult<T>(items, materialized.Count, page, pageSize);
    }
}
