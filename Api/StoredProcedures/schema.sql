CREATE TABLE Branches (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(120) NOT NULL,
    Address NVARCHAR(300) NULL,
    Latitude DECIMAL(10, 7) NULL,
    Longitude DECIMAL(10, 7) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE TABLE Departments (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(120) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE TABLE Positions (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    DepartmentId INT NOT NULL,
    Title NVARCHAR(120) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Positions_Departments FOREIGN KEY (DepartmentId) REFERENCES Departments(Id)
);

CREATE TABLE Employees (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    EmployeeCode NVARCHAR(20) NOT NULL UNIQUE,
    FullName NVARCHAR(180) NOT NULL,
    Gender NVARCHAR(20) NOT NULL,
    DateOfBirth DATE NOT NULL,
    Email NVARCHAR(180) NOT NULL UNIQUE,
    Phone NVARCHAR(40) NULL,
    DepartmentId INT NOT NULL,
    PositionId INT NOT NULL,
    BranchId INT NOT NULL,
    ManagerId INT NULL,
    ContractType NVARCHAR(60) NOT NULL,
    JoinDate DATE NOT NULL,
    ResignDate DATE NULL,
    Status NVARCHAR(30) NOT NULL,
    EmergencyContact NVARCHAR(180) NULL,
    EducationHistory NVARCHAR(MAX) NULL,
    WorkExperience NVARCHAR(MAX) NULL,
    BasicSalary DECIMAL(18, 2) NOT NULL DEFAULT 500,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NULL,
    CONSTRAINT FK_Employees_Departments FOREIGN KEY (DepartmentId) REFERENCES Departments(Id),
    CONSTRAINT FK_Employees_Positions FOREIGN KEY (PositionId) REFERENCES Positions(Id),
    CONSTRAINT FK_Employees_Branches FOREIGN KEY (BranchId) REFERENCES Branches(Id),
    CONSTRAINT FK_Employees_Manager FOREIGN KEY (ManagerId) REFERENCES Employees(Id)
);

CREATE TABLE Roles (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(80) NOT NULL UNIQUE
);

CREATE TABLE Permissions (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Code NVARCHAR(120) NOT NULL UNIQUE,
    Description NVARCHAR(240) NOT NULL
);

CREATE TABLE RolePermissions (
    RoleId INT NOT NULL,
    PermissionId INT NOT NULL,
    PRIMARY KEY (RoleId, PermissionId),
    CONSTRAINT FK_RolePermissions_Roles FOREIGN KEY (RoleId) REFERENCES Roles(Id),
    CONSTRAINT FK_RolePermissions_Permissions FOREIGN KEY (PermissionId) REFERENCES Permissions(Id)
);

CREATE TABLE Users (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    EmployeeId INT NULL,
    RoleId INT NOT NULL,
    Email NVARCHAR(180) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(400) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    MustChangePassword BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Users_Employees FOREIGN KEY (EmployeeId) REFERENCES Employees(Id),
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) REFERENCES Roles(Id)
);

CREATE TABLE Attendance (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    EmployeeId INT NOT NULL,
    WorkDate DATE NOT NULL,
    CheckIn DATETIME2 NULL,
    CheckOut DATETIME2 NULL,
    WorkMode NVARCHAR(40) NOT NULL,
    Status NVARCHAR(40) NOT NULL,
    Latitude DECIMAL(10, 7) NULL,
    Longitude DECIMAL(10, 7) NULL,
    LateMinutes INT NOT NULL DEFAULT 0,
    OvertimeMinutes INT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Attendance_Employees FOREIGN KEY (EmployeeId) REFERENCES Employees(Id),
    CONSTRAINT UQ_Attendance_EmployeeDate UNIQUE (EmployeeId, WorkDate)
);

CREATE TABLE LeaveRequests (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    EmployeeId INT NOT NULL,
    LeaveType NVARCHAR(60) NOT NULL,
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL,
    IsHalfDay BIT NOT NULL DEFAULT 0,
    Reason NVARCHAR(600) NOT NULL,
    AttachmentUrl NVARCHAR(500) NULL,
    Status NVARCHAR(40) NOT NULL,
    ManagerComment NVARCHAR(600) NULL,
    HrComment NVARCHAR(600) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NULL,
    CONSTRAINT FK_LeaveRequests_Employees FOREIGN KEY (EmployeeId) REFERENCES Employees(Id)
);

CREATE TABLE LeaveBalances (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    EmployeeId INT NOT NULL,
    LeaveType NVARCHAR(60) NOT NULL,
    [Year] INT NOT NULL,
    EntitledDays DECIMAL(6, 1) NOT NULL,
    CONSTRAINT FK_LeaveBalances_Employees FOREIGN KEY (EmployeeId) REFERENCES Employees(Id),
    CONSTRAINT UQ_LeaveBalances_EmployeeTypeYear UNIQUE (EmployeeId, LeaveType, [Year])
);

CREATE TABLE Payroll (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    EmployeeId INT NOT NULL,
    PeriodStart DATE NOT NULL,
    PeriodEnd DATE NOT NULL,
    BasicSalary DECIMAL(18, 2) NOT NULL,
    Allowance DECIMAL(18, 2) NOT NULL DEFAULT 0,
    Bonus DECIMAL(18, 2) NOT NULL DEFAULT 0,
    Tax DECIMAL(18, 2) NOT NULL DEFAULT 0,
    Deduction DECIMAL(18, 2) NOT NULL DEFAULT 0,
    OvertimePay DECIMAL(18, 2) NOT NULL DEFAULT 0,
    NetSalary AS (BasicSalary + Allowance + Bonus + OvertimePay - Tax - Deduction) PERSISTED,
    Status NVARCHAR(40) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Payroll_Employees FOREIGN KEY (EmployeeId) REFERENCES Employees(Id)
);

CREATE TABLE AuditLogs (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NULL,
    Action NVARCHAR(120) NOT NULL,
    EntityName NVARCHAR(120) NOT NULL,
    EntityId NVARCHAR(80) NULL,
    Details NVARCHAR(MAX) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE TABLE LoginSecurity (
    Email NVARCHAR(180) NOT NULL PRIMARY KEY,
    UserId INT NULL,
    FailedAccessCount INT NOT NULL DEFAULT 0,
    LockoutCycles INT NOT NULL DEFAULT 0,
    LockoutEndAt DATETIME2 NULL,
    RequiresAdminReset BIT NOT NULL DEFAULT 0,
    UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_LoginSecurity_Users FOREIGN KEY (UserId) REFERENCES Users(Id)
);

GO

CREATE OR ALTER PROCEDURE dbo.sp_GetDashboardSummary
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        (SELECT COUNT(*) FROM Employees WHERE Status = 'Active') AS TotalEmployees,
        (SELECT COUNT(*) FROM Attendance WHERE WorkDate = CONVERT(date, SYSUTCDATETIME()) AND CheckIn IS NOT NULL) AS TodayAttendance,
        (SELECT COUNT(*) FROM Attendance WHERE WorkDate = CONVERT(date, SYSUTCDATETIME()) AND LateMinutes > 0) AS LateEmployees,
        (SELECT COUNT(*) FROM LeaveRequests WHERE Status IN ('Pending', 'ManagerApproved')) AS PendingLeaveRequests;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_CreateLeaveRequest
    @EmployeeId INT,
    @LeaveType NVARCHAR(60),
    @StartDate DATE,
    @EndDate DATE,
    @IsHalfDay BIT,
    @Reason NVARCHAR(600)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO LeaveRequests (EmployeeId, LeaveType, StartDate, EndDate, IsHalfDay, Reason, Status)
    VALUES (@EmployeeId, @LeaveType, @StartDate, @EndDate, @IsHalfDay, @Reason, 'Pending');

    SELECT SCOPE_IDENTITY() AS LeaveRequestId;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_UpsertAttendance
    @EmployeeId INT,
    @WorkDate DATE,
    @CheckIn DATETIME2 = NULL,
    @CheckOut DATETIME2 = NULL,
    @WorkMode NVARCHAR(40),
    @Latitude DECIMAL(10,7) = NULL,
    @Longitude DECIMAL(10,7) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM Attendance WHERE EmployeeId = @EmployeeId AND WorkDate = @WorkDate)
    BEGIN
        UPDATE Attendance
        SET CheckOut = COALESCE(@CheckOut, CheckOut),
            Status = CASE WHEN COALESCE(@CheckOut, CheckOut) IS NULL THEN Status ELSE 'Present' END
        WHERE EmployeeId = @EmployeeId AND WorkDate = @WorkDate;
    END
    ELSE
    BEGIN
        INSERT INTO Attendance (EmployeeId, WorkDate, CheckIn, WorkMode, Status, Latitude, Longitude, LateMinutes)
        VALUES (
            @EmployeeId,
            @WorkDate,
            @CheckIn,
            @WorkMode,
            CASE WHEN DATEPART(HOUR, @CheckIn) > 8 OR (DATEPART(HOUR, @CheckIn) = 8 AND DATEPART(MINUTE, @CheckIn) > 30) THEN 'Late' ELSE 'Present' END,
            @Latitude,
            @Longitude,
            CASE WHEN @CheckIn > DATEADD(MINUTE, 510, CAST(@WorkDate AS DATETIME2)) THEN DATEDIFF(MINUTE, DATEADD(MINUTE, 510, CAST(@WorkDate AS DATETIME2)), @CheckIn) ELSE 0 END
        );
    END
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_ResetLoginSecurity
    @Email NVARCHAR(180)
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM LoginSecurity
    WHERE Email = LOWER(LTRIM(RTRIM(@Email)));
END;
GO
