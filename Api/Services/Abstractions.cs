using HrManagementSystem.Domain;

namespace HrManagementSystem.Application;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string storedHash);
}

public interface ITokenService
{
    string CreateToken(UserAccount user, string role, IReadOnlyCollection<string> permissions);
    bool TryValidate(string token, out UserContext? user);
}

public interface IHrRepository
{
    IReadOnlyList<Branch> Branches { get; }
    IReadOnlyList<Department> Departments { get; }
    IReadOnlyList<Position> Positions { get; }
    IReadOnlyList<Role> Roles { get; }
    IReadOnlyList<UserAccount> Users { get; }
    IReadOnlyList<Employee> Employees { get; }
    IReadOnlyList<AttendanceRecord> Attendance { get; }
    IReadOnlyList<LeaveRequest> LeaveRequests { get; }
    IReadOnlyList<PayrollRun> PayrollRuns { get; }
    IReadOnlyList<AuditLog> AuditLogs { get; }

    IReadOnlyCollection<string> GetPermissions(string roleName);
    LoginSecurityState GetLoginSecurity(string email);
    LoginSecurityState RecordFailedLogin(string email, int? userId);
    void ClearLoginSecurity(string email);
    Employee CreateEmployee(EmployeeWriteRequest request);
    Employee? UpdateEmployee(int id, EmployeeWriteRequest request);
    Employee? DeactivateEmployee(int id);
    UserAccount CreateUserAccount(int employeeId, int roleId, string email, string passwordHash, bool mustChangePassword);
    void UpdateUserPassword(int userId, string passwordHash, bool mustChangePassword);
    AttendanceRecord CheckIn(int employeeId, AttendanceCheckRequest request);
    AttendanceRecord? CheckOut(int employeeId);
    LeaveRequest CreateLeaveRequest(int employeeId, LeaveCreateRequest request);
    void AddAudit(int userId, string action, string entityName, string entityId, string details);
    IReadOnlyList<LeaveBalance> GetLeaveBalanceRows(int employeeId, int year);
    void EnsureDefaultLeaveBalances(int employeeId, int year);
    void UpsertLeaveBalance(int employeeId, string leaveType, int year, decimal entitledDays);
    PayrollRun CreatePayrollRun(PayrollRun run);
    PayrollRun? UpdatePayrollStatus(int id, PayrollStatus status);
}

public interface IAuthService
{
    LoginResult Login(LoginRequest request);
    CurrentUserView? GetCurrentUser(UserContext user);
    void ChangePassword(UserContext user, ChangePasswordRequest request);
    PasswordResetResult ForgotPassword(ForgotPasswordRequest request);
}

public interface IDashboardService
{
    DashboardView GetDashboard();
}

public interface IReferenceDataService
{
    IEnumerable<Department> GetDepartments();
    IEnumerable<Position> GetPositions();
    IEnumerable<Branch> GetBranches();
}

public interface IEmployeeService
{
    PagedResult<EmployeeView> GetEmployees(UserContext user, ListQuery query);
    EmployeeView? GetEmployee(int id, UserContext user);
    EmployeeCreateResult CreateEmployee(EmployeeWriteRequest request, UserContext user);
    EmployeeView? UpdateEmployee(int id, EmployeeWriteRequest request, UserContext user);
    bool DeactivateEmployee(int id, UserContext user);
    PasswordResetResult ResetPassword(int employeeId, UserContext user);
}

public interface IAttendanceService
{
    PagedResult<AttendanceView> GetAttendance(UserContext user, ListQuery query);
    AttendanceView CheckIn(AttendanceCheckRequest request, UserContext user);
    AttendanceView? CheckOut(AttendanceCheckRequest request, UserContext user);
}

public interface ILeaveService
{
    PagedResult<LeaveView> GetLeaveRequests(UserContext user, ListQuery query);
    LeaveView CreateLeaveRequest(LeaveCreateRequest request, UserContext user);
    LeaveView? DecideLeave(int id, LeaveDecisionRequest request, UserContext user);
    IReadOnlyList<LeaveBalanceView> GetLeaveBalances(UserContext user, int? employeeId, int? year);
    LeaveBalanceView UpsertLeaveBalance(LeaveBalanceUpsertRequest request, UserContext user);
}

public interface IRoleService
{
    IEnumerable<RoleView> GetRoles();
}

public interface IPayrollService
{
    PagedResult<PayrollView> GetPayroll(UserContext user, ListQuery query);
    PayrollGenerateResult GeneratePayroll(PayrollGenerateRequest request, UserContext user);
    PayrollView? UpdatePayrollStatus(int id, PayrollStatusUpdateRequest request, UserContext user);
}
