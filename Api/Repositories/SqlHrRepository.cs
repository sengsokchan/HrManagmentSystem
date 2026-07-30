using HrManagementSystem.Application;
using HrManagementSystem.Application.Services;
using HrManagementSystem.Domain;
using Microsoft.Data.SqlClient;

namespace HrManagementSystem.Infrastructure.Persistence;

public sealed class SqlHrRepository : IHrRepository, ILeaveDecisionRepository
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromSeconds(30);
    private readonly string _connectionString;
    private readonly IBusinessClock _clock;

    private List<Branch>? _branches;
    private List<Department>? _departments;
    private List<Position>? _positions;
    private List<Role>? _roles;
    private List<UserAccount>? _users;
    private List<Employee>? _employees;
    private List<AttendanceRecord>? _attendance;
    private List<LeaveRequest>? _leaveRequests;
    private List<PayrollRun>? _payrollRuns;
    private List<AuditLog>? _auditLogs;

    public SqlHrRepository(IConfiguration configuration, IBusinessClock clock)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");
        _clock = clock;
        EnsureLoginSecurityTable();
        EnsureMustChangePasswordColumn();
        EnsureLeaveBalancesTable();
        EnsureEmployeeBasicSalaryColumn();
        NormalizePayrollStatuses();
        EnsurePayrollWritePermission();
    }

    public IReadOnlyList<Branch> Branches => _branches ??= Query(
        "SELECT Id, Name, Address, Latitude, Longitude FROM Branches ORDER BY Name",
        reader => new Branch(reader.GetInt32(0), reader.GetString(1), NullableString(reader, 2), NullableDecimal(reader, 3) ?? 0, NullableDecimal(reader, 4) ?? 0));

    public IReadOnlyList<Department> Departments => _departments ??= Query(
        "SELECT Id, Name FROM Departments ORDER BY Name",
        reader => new Department(reader.GetInt32(0), reader.GetString(1)));

    public IReadOnlyList<Position> Positions => _positions ??= Query(
        "SELECT Id, DepartmentId, Title FROM Positions ORDER BY Title",
        reader => new Position(reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2)));

    public IReadOnlyList<Role> Roles => _roles ??= Query(
        "SELECT Id, Name FROM Roles ORDER BY Name",
        reader => new Role(reader.GetInt32(0), reader.GetString(1)));

    public IReadOnlyList<UserAccount> Users => _users ??= Query(
        "SELECT Id, EmployeeId, RoleId, Email, PasswordHash, IsActive, MustChangePassword FROM Users ORDER BY Id",
        reader => new UserAccount(
            reader.GetInt32(0),
            NullableInt(reader, 1),
            reader.GetInt32(2),
            reader.GetString(3),
            reader.GetString(4),
            reader.GetBoolean(5),
            reader.GetBoolean(6)));

    public IReadOnlyList<Employee> Employees => _employees ??= Query(
        """
        SELECT Id, EmployeeCode, FullName, Gender, DateOfBirth, Email, Phone, DepartmentId, PositionId,
               BranchId, ManagerId, ContractType, JoinDate, ResignDate, Status, EmergencyContact,
               EducationHistory, WorkExperience, ISNULL(BasicSalary, 500), CreatedAt, UpdatedAt
        FROM Employees
        ORDER BY Id DESC
        """,
        reader => new Employee(
            reader.GetInt32(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            DateOnly.FromDateTime(reader.GetDateTime(4)),
            reader.GetString(5),
            NullableString(reader, 6),
            reader.GetInt32(7),
            reader.GetInt32(8),
            reader.GetInt32(9),
            NullableInt(reader, 10),
            reader.GetString(11),
            DateOnly.FromDateTime(reader.GetDateTime(12)),
            NullableDateOnly(reader, 13),
            ParseEnum<EmployeeStatus>(reader.GetString(14)),
            NullableString(reader, 15),
            NullableString(reader, 16),
            NullableString(reader, 17),
            reader.GetDecimal(18),
            reader.GetDateTime(19),
            NullableDateTime(reader, 20)));

    public IReadOnlyList<AttendanceRecord> Attendance => _attendance ??= Query(
        """
        SELECT Id, EmployeeId, WorkDate, CheckIn, CheckOut, Status, Latitude, Longitude, WorkMode, LateMinutes, OvertimeMinutes
        FROM Attendance
        ORDER BY WorkDate DESC, EmployeeId
        """,
        reader => new AttendanceRecord(
            reader.GetInt32(0),
            reader.GetInt32(1),
            DateOnly.FromDateTime(reader.GetDateTime(2)),
            NullableDateTime(reader, 3),
            NullableDateTime(reader, 4),
            ParseEnum<AttendanceStatus>(reader.GetString(5)),
            NullableDecimal(reader, 6),
            NullableDecimal(reader, 7),
            reader.GetString(8),
            reader.GetInt32(9),
            reader.GetInt32(10)));

    public IReadOnlyList<LeaveRequest> LeaveRequests => _leaveRequests ??= Query(
        """
        SELECT Id, EmployeeId, LeaveType, StartDate, EndDate, IsHalfDay, Reason, AttachmentUrl, Status,
               ManagerComment, HrComment, CreatedAt, UpdatedAt
        FROM LeaveRequests
        ORDER BY CreatedAt DESC
        """,
        reader => MapLeave(reader));

    public IReadOnlyList<PayrollRun> PayrollRuns => _payrollRuns ??= Query(
        """
        SELECT Id, EmployeeId, PeriodStart, PeriodEnd, BasicSalary, Allowance, Bonus, Tax, Deduction, OvertimePay, Status
        FROM Payroll
        ORDER BY PeriodEnd DESC
        """,
        reader => new PayrollRun(
            reader.GetInt32(0),
            reader.GetInt32(1),
            DateOnly.FromDateTime(reader.GetDateTime(2)),
            DateOnly.FromDateTime(reader.GetDateTime(3)),
            reader.GetDecimal(4),
            reader.GetDecimal(5),
            reader.GetDecimal(6),
            reader.GetDecimal(7),
            reader.GetDecimal(8),
            reader.GetDecimal(9),
            ParseEnum<PayrollStatus>(reader.GetString(10))));

    public IReadOnlyList<AuditLog> AuditLogs => _auditLogs ??= Query(
        "SELECT Id, UserId, Action, EntityName, EntityId, Details, CreatedAt FROM AuditLogs ORDER BY CreatedAt DESC",
        reader => new AuditLog(
            reader.GetInt64(0),
            NullableInt(reader, 1) ?? 0,
            reader.GetString(2),
            reader.GetString(3),
            NullableString(reader, 4),
            NullableString(reader, 5),
            reader.GetDateTime(6)));

    public IReadOnlyCollection<string> GetPermissions(string roleName) => Query(
        """
        SELECT p.Code
        FROM Permissions p
        INNER JOIN RolePermissions rp ON rp.PermissionId = p.Id
        INNER JOIN Roles r ON r.Id = rp.RoleId
        WHERE r.Name = @RoleName
        ORDER BY p.Code
        """,
        reader => reader.GetString(0),
        new SqlParameter("@RoleName", roleName));

    public LoginSecurityState GetLoginSecurity(string email)
    {
        var normalizedEmail = NormalizeEmail(email);
        return Query(
            """
            SELECT Email, FailedAccessCount, LockoutCycles, LockoutEndAt, RequiresAdminReset
            FROM LoginSecurity
            WHERE Email = @Email
            """,
            reader => new LoginSecurityState(
                reader.GetString(0),
                reader.GetInt32(1),
                reader.GetInt32(2),
                NullableDateTime(reader, 3),
                reader.GetBoolean(4)),
            new SqlParameter("@Email", normalizedEmail))
            .FirstOrDefault() ?? new LoginSecurityState(normalizedEmail, 0, 0, null, false);
    }

    public LoginSecurityState RecordFailedLogin(string email, int? userId)
    {
        var normalizedEmail = NormalizeEmail(email);
        var current = GetLoginSecurity(normalizedEmail);
        if (current.RequiresAdminReset) return current;

        var lockExpired = current.LockoutEndAt is not null && current.LockoutEndAt <= DateTime.UtcNow;
        var failedAccessCount = lockExpired ? 1 : current.FailedAccessCount + 1;
        var lockoutCycles = current.LockoutCycles;
        DateTime? lockoutEndAt = lockExpired ? null : current.LockoutEndAt;
        var requiresAdminReset = false;

        if (failedAccessCount >= MaxFailedAttempts)
        {
            if (lockoutCycles >= 1)
            {
                requiresAdminReset = true;
                lockoutEndAt = null;
            }
            else
            {
                lockoutCycles++;
                lockoutEndAt = DateTime.UtcNow.Add(LockoutDuration);
            }
        }

        Execute(
            """
            MERGE LoginSecurity AS target
            USING (SELECT @Email AS Email) AS source
            ON target.Email = source.Email
            WHEN MATCHED THEN
                UPDATE SET UserId = @UserId,
                           FailedAccessCount = @FailedAccessCount,
                           LockoutCycles = @LockoutCycles,
                           LockoutEndAt = @LockoutEndAt,
                           RequiresAdminReset = @RequiresAdminReset,
                           UpdatedAt = SYSUTCDATETIME()
            WHEN NOT MATCHED THEN
                INSERT (Email, UserId, FailedAccessCount, LockoutCycles, LockoutEndAt, RequiresAdminReset)
                VALUES (@Email, @UserId, @FailedAccessCount, @LockoutCycles, @LockoutEndAt, @RequiresAdminReset);
            """,
            new SqlParameter("@Email", normalizedEmail),
            new SqlParameter("@UserId", DbValue(userId)),
            new SqlParameter("@FailedAccessCount", failedAccessCount),
            new SqlParameter("@LockoutCycles", lockoutCycles),
            new SqlParameter("@LockoutEndAt", DbValue(lockoutEndAt)),
            new SqlParameter("@RequiresAdminReset", requiresAdminReset));

        return new LoginSecurityState(normalizedEmail, failedAccessCount, lockoutCycles, lockoutEndAt, requiresAdminReset);
    }

    public void ClearLoginSecurity(string email) =>
        Execute("DELETE FROM LoginSecurity WHERE Email = @Email", new SqlParameter("@Email", NormalizeEmail(email)));

    public Employee CreateEmployee(EmployeeWriteRequest request)
    {
        var nextId = Scalar<int>("SELECT ISNULL(MAX(Id), 0) + 1 FROM Employees");
        var code = $"EMP{nextId:0000}";
        var id = Scalar<int>(
            """
            INSERT INTO Employees
                (EmployeeCode, FullName, Gender, DateOfBirth, Email, Phone, DepartmentId, PositionId, BranchId, ManagerId,
                 ContractType, JoinDate, ResignDate, Status, EmergencyContact, EducationHistory, WorkExperience, BasicSalary)
            OUTPUT INSERTED.Id
            VALUES
                (@EmployeeCode, @FullName, @Gender, @DateOfBirth, @Email, @Phone, @DepartmentId, @PositionId, @BranchId, @ManagerId,
                 @ContractType, @JoinDate, @ResignDate, @Status, @EmergencyContact, @EducationHistory, @WorkExperience, @BasicSalary)
            """,
            EmployeeParameters(request, code));

        InvalidateCachedLists();
        return Employees.First(e => e.Id == id);
    }

    public Employee? UpdateEmployee(int id, EmployeeWriteRequest request)
    {
        var existing = Employees.FirstOrDefault(e => e.Id == id);
        if (existing is null) return null;

        Execute(
            """
            UPDATE Employees
            SET FullName = @FullName,
                Gender = @Gender,
                DateOfBirth = @DateOfBirth,
                Email = @Email,
                Phone = @Phone,
                DepartmentId = @DepartmentId,
                PositionId = @PositionId,
                BranchId = @BranchId,
                ManagerId = @ManagerId,
                ContractType = @ContractType,
                JoinDate = @JoinDate,
                ResignDate = @ResignDate,
                Status = @Status,
                EmergencyContact = @EmergencyContact,
                EducationHistory = @EducationHistory,
                WorkExperience = @WorkExperience,
                BasicSalary = @BasicSalary,
                UpdatedAt = SYSUTCDATETIME()
            WHERE Id = @Id
            """,
            EmployeeParameters(request, existing.EmployeeCode).Append(new SqlParameter("@Id", id)).ToArray());

        InvalidateCachedLists();
        return Employees.FirstOrDefault(e => e.Id == id);
    }

    public Employee? DeactivateEmployee(int id)
    {
        Execute("UPDATE Employees SET Status = 'Inactive', UpdatedAt = SYSUTCDATETIME() WHERE Id = @Id", new SqlParameter("@Id", id));
        InvalidateCachedLists();
        return Employees.FirstOrDefault(e => e.Id == id);
    }

    public UserAccount CreateUserAccount(int employeeId, int roleId, string email, string passwordHash, bool mustChangePassword)
    {
        var id = Scalar<int>(
            """
            INSERT INTO Users (EmployeeId, RoleId, Email, PasswordHash, IsActive, MustChangePassword)
            OUTPUT INSERTED.Id
            VALUES (@EmployeeId, @RoleId, @Email, @PasswordHash, 1, @MustChangePassword)
            """,
            new SqlParameter("@EmployeeId", employeeId),
            new SqlParameter("@RoleId", roleId),
            new SqlParameter("@Email", email.Trim()),
            new SqlParameter("@PasswordHash", passwordHash),
            new SqlParameter("@MustChangePassword", mustChangePassword));

        InvalidateCachedLists();
        return Users.First(u => u.Id == id);
    }

    public void UpdateUserPassword(int userId, string passwordHash, bool mustChangePassword)
    {
        Execute(
            """
            UPDATE Users
            SET PasswordHash = @PasswordHash,
                MustChangePassword = @MustChangePassword
            WHERE Id = @Id
            """,
            new SqlParameter("@PasswordHash", passwordHash),
            new SqlParameter("@MustChangePassword", mustChangePassword),
            new SqlParameter("@Id", userId));
        InvalidateCachedLists();
    }

    public AttendanceRecord CheckIn(int employeeId, AttendanceCheckRequest request)
    {
        var now = _clock.LocalNow;
        var today = _clock.Today;
        var existing = Attendance.FirstOrDefault(a => a.EmployeeId == employeeId && a.WorkDate == today);
        if (existing is not null) return existing;

        var officeStart = new DateTime(now.Year, now.Month, now.Day, 8, 30, 0);
        var lateMinutes = now > officeStart ? (int)(now - officeStart).TotalMinutes : 0;
        var status = lateMinutes > 0 ? AttendanceStatus.Late : AttendanceStatus.Present;
        var id = Scalar<int>(
            """
            INSERT INTO Attendance (EmployeeId, WorkDate, CheckIn, WorkMode, Status, Latitude, Longitude, LateMinutes, OvertimeMinutes)
            OUTPUT INSERTED.Id
            VALUES (@EmployeeId, @WorkDate, @CheckIn, @WorkMode, @Status, @Latitude, @Longitude, @LateMinutes, 0)
            """,
            new SqlParameter("@EmployeeId", employeeId),
            new SqlParameter("@WorkDate", today.ToDateTime(TimeOnly.MinValue)),
            new SqlParameter("@CheckIn", now),
            new SqlParameter("@WorkMode", string.IsNullOrWhiteSpace(request.WorkMode) ? "Office" : request.WorkMode.Trim()),
            new SqlParameter("@Status", status.ToString()),
            new SqlParameter("@Latitude", DbValue(request.Latitude)),
            new SqlParameter("@Longitude", DbValue(request.Longitude)),
            new SqlParameter("@LateMinutes", lateMinutes));

        InvalidateCachedLists();
        return Attendance.First(a => a.Id == id);
    }

    public AttendanceRecord? CheckOut(int employeeId)
    {
        var now = _clock.LocalNow;
        var today = _clock.Today;
        var officeEnd = new DateTime(now.Year, now.Month, now.Day, 17, 30, 0);
        var overtimeMinutes = now > officeEnd ? (int)(now - officeEnd).TotalMinutes : 0;

        Execute(
            """
            UPDATE Attendance
            SET CheckOut = @CheckOut, OvertimeMinutes = @OvertimeMinutes
            WHERE EmployeeId = @EmployeeId AND WorkDate = @WorkDate
            """,
            new SqlParameter("@CheckOut", now),
            new SqlParameter("@OvertimeMinutes", overtimeMinutes),
            new SqlParameter("@EmployeeId", employeeId),
            new SqlParameter("@WorkDate", today.ToDateTime(TimeOnly.MinValue)));

        InvalidateCachedLists();
        return Attendance.FirstOrDefault(a => a.EmployeeId == employeeId && a.WorkDate == today);
    }

    public LeaveRequest CreateLeaveRequest(int employeeId, LeaveCreateRequest request)
    {
        var id = Scalar<int>(
            """
            INSERT INTO LeaveRequests (EmployeeId, LeaveType, StartDate, EndDate, IsHalfDay, Reason, AttachmentUrl, Status)
            OUTPUT INSERTED.Id
            VALUES (@EmployeeId, @LeaveType, @StartDate, @EndDate, @IsHalfDay, @Reason, @AttachmentUrl, 'Pending')
            """,
            new SqlParameter("@EmployeeId", employeeId),
            new SqlParameter("@LeaveType", request.LeaveType.Trim()),
            new SqlParameter("@StartDate", request.StartDate.ToDateTime(TimeOnly.MinValue)),
            new SqlParameter("@EndDate", request.EndDate.ToDateTime(TimeOnly.MinValue)),
            new SqlParameter("@IsHalfDay", request.IsHalfDay),
            new SqlParameter("@Reason", request.Reason.Trim()),
            new SqlParameter("@AttachmentUrl", DbValue(request.AttachmentUrl)));

        InvalidateCachedLists();
        return LeaveRequests.First(l => l.Id == id);
    }

    public LeaveRequest? UpdateLeaveDecision(int id, LeaveStatus status, string? comment, string actorRole)
    {
        var commentColumn = actorRole == "Manager" ? "ManagerComment" : "HrComment";
        Execute(
            $"""
            UPDATE LeaveRequests
            SET Status = @Status,
                {commentColumn} = @Comment,
                UpdatedAt = SYSUTCDATETIME()
            WHERE Id = @Id
            """,
            new SqlParameter("@Id", id),
            new SqlParameter("@Status", status.ToString()),
            new SqlParameter("@Comment", DbValue(comment)));

        InvalidateCachedLists();
        return LeaveRequests.FirstOrDefault(l => l.Id == id);
    }

    public void AddAudit(int userId, string action, string entityName, string entityId, string details)
    {
        Execute(
            "INSERT INTO AuditLogs (UserId, Action, EntityName, EntityId, Details) VALUES (@UserId, @Action, @EntityName, @EntityId, @Details)",
            new SqlParameter("@UserId", userId),
            new SqlParameter("@Action", action),
            new SqlParameter("@EntityName", entityName),
            new SqlParameter("@EntityId", entityId),
            new SqlParameter("@Details", details));
        _auditLogs = null;
    }

    public IReadOnlyList<LeaveBalance> GetLeaveBalanceRows(int employeeId, int year) =>
        Query(
            """
            SELECT Id, EmployeeId, LeaveType, [Year], EntitledDays
            FROM LeaveBalances
            WHERE EmployeeId = @EmployeeId AND [Year] = @Year
            ORDER BY LeaveType
            """,
            reader => new LeaveBalance(
                reader.GetInt32(0),
                reader.GetInt32(1),
                reader.GetString(2),
                reader.GetInt32(3),
                reader.GetDecimal(4)),
            new SqlParameter("@EmployeeId", employeeId),
            new SqlParameter("@Year", year));

    public void EnsureDefaultLeaveBalances(int employeeId, int year)
    {
        foreach (var (leaveType, entitledDays) in LeaveCalculator.DefaultEntitlements)
        {
            Execute(
                """
                IF NOT EXISTS (
                    SELECT 1 FROM LeaveBalances
                    WHERE EmployeeId = @EmployeeId AND LeaveType = @LeaveType AND [Year] = @Year)
                BEGIN
                    INSERT INTO LeaveBalances (EmployeeId, LeaveType, [Year], EntitledDays)
                    VALUES (@EmployeeId, @LeaveType, @Year, @EntitledDays)
                END
                """,
                new SqlParameter("@EmployeeId", employeeId),
                new SqlParameter("@LeaveType", leaveType),
                new SqlParameter("@Year", year),
                new SqlParameter("@EntitledDays", entitledDays));
        }
    }

    public void UpsertLeaveBalance(int employeeId, string leaveType, int year, decimal entitledDays) =>
        Execute(
            """
            MERGE LeaveBalances AS target
            USING (SELECT @EmployeeId AS EmployeeId, @LeaveType AS LeaveType, @Year AS [Year]) AS source
            ON target.EmployeeId = source.EmployeeId
               AND target.LeaveType = source.LeaveType
               AND target.[Year] = source.[Year]
            WHEN MATCHED THEN
                UPDATE SET EntitledDays = @EntitledDays
            WHEN NOT MATCHED THEN
                INSERT (EmployeeId, LeaveType, [Year], EntitledDays)
                VALUES (@EmployeeId, @LeaveType, @Year, @EntitledDays);
            """,
            new SqlParameter("@EmployeeId", employeeId),
            new SqlParameter("@LeaveType", leaveType.Trim()),
            new SqlParameter("@Year", year),
            new SqlParameter("@EntitledDays", entitledDays));

    public PayrollRun CreatePayrollRun(PayrollRun run)
    {
        var id = Scalar<int>(
            """
            INSERT INTO Payroll
                (EmployeeId, PeriodStart, PeriodEnd, BasicSalary, Allowance, Bonus, Tax, Deduction, OvertimePay, Status)
            OUTPUT INSERTED.Id
            VALUES
                (@EmployeeId, @PeriodStart, @PeriodEnd, @BasicSalary, @Allowance, @Bonus, @Tax, @Deduction, @OvertimePay, @Status)
            """,
            new SqlParameter("@EmployeeId", run.EmployeeId),
            new SqlParameter("@PeriodStart", run.PeriodStart.ToDateTime(TimeOnly.MinValue)),
            new SqlParameter("@PeriodEnd", run.PeriodEnd.ToDateTime(TimeOnly.MinValue)),
            new SqlParameter("@BasicSalary", run.BasicSalary),
            new SqlParameter("@Allowance", run.Allowance),
            new SqlParameter("@Bonus", run.Bonus),
            new SqlParameter("@Tax", run.Tax),
            new SqlParameter("@Deduction", run.Deduction),
            new SqlParameter("@OvertimePay", run.OvertimePay),
            new SqlParameter("@Status", run.Status.ToString()));

        InvalidateCachedLists();
        return PayrollRuns.First(p => p.Id == id);
    }

    public PayrollRun? UpdatePayrollStatus(int id, PayrollStatus status)
    {
        Execute(
            "UPDATE Payroll SET Status = @Status WHERE Id = @Id",
            new SqlParameter("@Id", id),
            new SqlParameter("@Status", status.ToString()));
        InvalidateCachedLists();
        return PayrollRuns.FirstOrDefault(p => p.Id == id);
    }

    private void InvalidateCachedLists()
    {
        _branches = null;
        _departments = null;
        _positions = null;
        _roles = null;
        _users = null;
        _employees = null;
        _attendance = null;
        _leaveRequests = null;
        _payrollRuns = null;
        _auditLogs = null;
    }

    private void EnsureLoginSecurityTable() => Execute(
        """
        IF OBJECT_ID('dbo.LoginSecurity', 'U') IS NULL
        BEGIN
            CREATE TABLE dbo.LoginSecurity (
                Email NVARCHAR(180) NOT NULL PRIMARY KEY,
                UserId INT NULL,
                FailedAccessCount INT NOT NULL DEFAULT 0,
                LockoutCycles INT NOT NULL DEFAULT 0,
                LockoutEndAt DATETIME2 NULL,
                RequiresAdminReset BIT NOT NULL DEFAULT 0,
                UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
            );
        END
        """);

    private void EnsureMustChangePasswordColumn() => Execute(
        """
        IF COL_LENGTH('dbo.Users', 'MustChangePassword') IS NULL
        BEGIN
            ALTER TABLE dbo.Users
            ADD MustChangePassword BIT NOT NULL
                CONSTRAINT DF_Users_MustChangePassword DEFAULT 0;
        END
        """);

    private void EnsureLeaveBalancesTable() => Execute(
        """
        IF OBJECT_ID('dbo.LeaveBalances', 'U') IS NULL
        BEGIN
            CREATE TABLE dbo.LeaveBalances (
                Id INT IDENTITY(1,1) PRIMARY KEY,
                EmployeeId INT NOT NULL,
                LeaveType NVARCHAR(60) NOT NULL,
                [Year] INT NOT NULL,
                EntitledDays DECIMAL(6, 1) NOT NULL,
                CONSTRAINT FK_LeaveBalances_Employees FOREIGN KEY (EmployeeId) REFERENCES Employees(Id),
                CONSTRAINT UQ_LeaveBalances_EmployeeTypeYear UNIQUE (EmployeeId, LeaveType, [Year])
            );
        END
        """);

    private void EnsureEmployeeBasicSalaryColumn() => Execute(
        """
        IF COL_LENGTH('dbo.Employees', 'BasicSalary') IS NULL
        BEGIN
            ALTER TABLE dbo.Employees
            ADD BasicSalary DECIMAL(18, 2) NOT NULL
                CONSTRAINT DF_Employees_BasicSalary DEFAULT 500;
        END
        """);

    private void NormalizePayrollStatuses() => Execute(
        """
        UPDATE Payroll SET Status = 'Approved' WHERE Status IN ('Processed', 'Ready');
        """);

    private void EnsurePayrollWritePermission() => Execute(
        """
        IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Code = 'payroll.write')
        BEGIN
            INSERT INTO Permissions (Code, Description) VALUES ('payroll.write', 'Generate and update payroll status');
        END

        DECLARE @PermissionId INT = (SELECT Id FROM Permissions WHERE Code = 'payroll.write');
        DECLARE @HrAdminRoleId INT = (SELECT Id FROM Roles WHERE Name = 'HR Admin');
        IF @PermissionId IS NOT NULL AND @HrAdminRoleId IS NOT NULL
           AND NOT EXISTS (SELECT 1 FROM RolePermissions WHERE RoleId = @HrAdminRoleId AND PermissionId = @PermissionId)
        BEGIN
            INSERT INTO RolePermissions (RoleId, PermissionId) VALUES (@HrAdminRoleId, @PermissionId);
        END
        """);

    private List<T> Query<T>(string sql, Func<SqlDataReader, T> map, params SqlParameter[] parameters)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddRange(parameters);
        connection.Open();
        using var reader = command.ExecuteReader();
        var items = new List<T>();
        while (reader.Read()) items.Add(map(reader));
        return items;
    }

    private T Scalar<T>(string sql, params SqlParameter[] parameters)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddRange(parameters);
        connection.Open();
        var value = command.ExecuteScalar();
        return value is null or DBNull ? default! : (T)Convert.ChangeType(value, typeof(T));
    }

    private void Execute(string sql, params SqlParameter[] parameters)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddRange(parameters);
        connection.Open();
        command.ExecuteNonQuery();
    }

    private static LeaveRequest MapLeave(SqlDataReader reader) =>
        new(
            reader.GetInt32(0),
            reader.GetInt32(1),
            reader.GetString(2),
            DateOnly.FromDateTime(reader.GetDateTime(3)),
            DateOnly.FromDateTime(reader.GetDateTime(4)),
            reader.GetBoolean(5),
            reader.GetString(6),
            NullableStringOrNull(reader, 7),
            ParseEnum<LeaveStatus>(reader.GetString(8)),
            NullableStringOrNull(reader, 9),
            NullableStringOrNull(reader, 10),
            reader.GetDateTime(11),
            NullableDateTime(reader, 12));

    private static SqlParameter[] EmployeeParameters(EmployeeWriteRequest request, string employeeCode) =>
    [
        new SqlParameter("@EmployeeCode", employeeCode),
        new SqlParameter("@FullName", request.FullName.Trim()),
        new SqlParameter("@Gender", request.Gender.Trim()),
        new SqlParameter("@DateOfBirth", request.DateOfBirth.ToDateTime(TimeOnly.MinValue)),
        new SqlParameter("@Email", request.Email.Trim()),
        new SqlParameter("@Phone", DbValue(request.Phone)),
        new SqlParameter("@DepartmentId", request.DepartmentId),
        new SqlParameter("@PositionId", request.PositionId),
        new SqlParameter("@BranchId", request.BranchId),
        new SqlParameter("@ManagerId", DbValue(request.ManagerId)),
        new SqlParameter("@ContractType", request.ContractType.Trim()),
        new SqlParameter("@JoinDate", request.JoinDate.ToDateTime(TimeOnly.MinValue)),
        new SqlParameter("@ResignDate", DbValue(request.ResignDate?.ToDateTime(TimeOnly.MinValue))),
        new SqlParameter("@Status", request.Status.ToString()),
        new SqlParameter("@EmergencyContact", DbValue(request.EmergencyContact)),
        new SqlParameter("@EducationHistory", DbValue(request.EducationHistory)),
        new SqlParameter("@WorkExperience", DbValue(request.WorkExperience)),
        new SqlParameter("@BasicSalary", request.BasicSalary < 0 ? 0 : request.BasicSalary)
    ];

    private static object DbValue<T>(T? value) => value is null ? DBNull.Value : value;
    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
    private static int? NullableInt(SqlDataReader reader, int index) => reader.IsDBNull(index) ? null : reader.GetInt32(index);
    private static decimal? NullableDecimal(SqlDataReader reader, int index) => reader.IsDBNull(index) ? null : reader.GetDecimal(index);
    private static DateTime? NullableDateTime(SqlDataReader reader, int index) => reader.IsDBNull(index) ? null : reader.GetDateTime(index);
    private static DateOnly? NullableDateOnly(SqlDataReader reader, int index) => reader.IsDBNull(index) ? null : DateOnly.FromDateTime(reader.GetDateTime(index));
    private static string NullableString(SqlDataReader reader, int index) => reader.IsDBNull(index) ? string.Empty : reader.GetString(index);
    private static string? NullableStringOrNull(SqlDataReader reader, int index) => reader.IsDBNull(index) ? null : reader.GetString(index);
    private static TEnum ParseEnum<TEnum>(string value) where TEnum : struct, Enum
    {
        var normalized = value.Replace(" ", string.Empty);
        if (typeof(TEnum) == typeof(PayrollStatus) &&
            normalized.Equals("Processed", StringComparison.OrdinalIgnoreCase))
        {
            normalized = nameof(PayrollStatus.Approved);
        }

        return Enum.TryParse(normalized, true, out TEnum parsed) ? parsed : default;
    }
}
