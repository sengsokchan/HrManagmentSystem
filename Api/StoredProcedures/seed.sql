-- Seed demo data for local development
SET NOCOUNT ON;

IF NOT EXISTS (SELECT 1 FROM Branches)
BEGIN
    INSERT INTO Branches (Name, Address, Latitude, Longitude)
    VALUES (N'Phnom Penh HQ', N'Street 271, Phnom Penh', 11.5564000, 104.9282000);
END

IF NOT EXISTS (SELECT 1 FROM Departments)
BEGIN
    INSERT INTO Departments (Name) VALUES (N'Human Resources'), (N'Engineering'), (N'Operations');
END

IF NOT EXISTS (SELECT 1 FROM Positions)
BEGIN
    DECLARE @HrDept INT = (SELECT TOP 1 Id FROM Departments WHERE Name = N'Human Resources');
    DECLARE @EngDept INT = (SELECT TOP 1 Id FROM Departments WHERE Name = N'Engineering');
    DECLARE @OpsDept INT = (SELECT TOP 1 Id FROM Departments WHERE Name = N'Operations');

    INSERT INTO Positions (DepartmentId, Title) VALUES
        (@HrDept, N'HR Admin'),
        (@EngDept, N'Software Engineer'),
        (@OpsDept, N'Team Manager');
END

IF NOT EXISTS (SELECT 1 FROM Roles WHERE Name = N'Employee')
    INSERT INTO Roles (Name) VALUES (N'Employee');
IF NOT EXISTS (SELECT 1 FROM Roles WHERE Name = N'Manager')
    INSERT INTO Roles (Name) VALUES (N'Manager');
IF NOT EXISTS (SELECT 1 FROM Roles WHERE Name = N'HR Admin')
    INSERT INTO Roles (Name) VALUES (N'HR Admin');

MERGE Permissions AS target
USING (VALUES
    (N'employees.read', N'Read employees'),
    (N'employees.write', N'Create/update employees'),
    (N'attendance.read', N'Read attendance'),
    (N'attendance.write', N'Check-in/out for others'),
    (N'leave.read', N'Read leave requests'),
    (N'leave.write', N'Manage leave balances'),
    (N'leave.approve.manager', N'Manager leave approval'),
    (N'leave.approve.hr', N'HR leave approval'),
    (N'payroll.read', N'Read payroll'),
    (N'payroll.write', N'Generate/update payroll'),
    (N'roles.read', N'Read roles')
) AS source (Code, Description)
ON target.Code = source.Code
WHEN NOT MATCHED THEN
    INSERT (Code, Description) VALUES (source.Code, source.Description);

DECLARE @EmployeeRoleId INT = (SELECT Id FROM Roles WHERE Name = N'Employee');
DECLARE @ManagerRoleId INT = (SELECT Id FROM Roles WHERE Name = N'Manager');
DECLARE @HrAdminRoleId INT = (SELECT Id FROM Roles WHERE Name = N'HR Admin');

-- Employee: self-service only (own records via employeeId filters)
-- Manager
INSERT INTO RolePermissions (RoleId, PermissionId)
SELECT @ManagerRoleId, p.Id
FROM Permissions p
WHERE p.Code IN (N'employees.read', N'attendance.read', N'leave.read', N'leave.approve.manager', N'payroll.read')
  AND NOT EXISTS (
      SELECT 1 FROM RolePermissions rp
      WHERE rp.RoleId = @ManagerRoleId AND rp.PermissionId = p.Id);

-- HR Admin: all
INSERT INTO RolePermissions (RoleId, PermissionId)
SELECT @HrAdminRoleId, p.Id
FROM Permissions p
WHERE NOT EXISTS (
    SELECT 1 FROM RolePermissions rp
    WHERE rp.RoleId = @HrAdminRoleId AND rp.PermissionId = p.Id);

DECLARE @BranchId INT = (SELECT TOP 1 Id FROM Branches ORDER BY Id);
DECLARE @HrDeptId INT = (SELECT TOP 1 Id FROM Departments WHERE Name = N'Human Resources');
DECLARE @EngDeptId INT = (SELECT TOP 1 Id FROM Departments WHERE Name = N'Engineering');
DECLARE @OpsDeptId INT = (SELECT TOP 1 Id FROM Departments WHERE Name = N'Operations');
DECLARE @HrPosId INT = (SELECT TOP 1 Id FROM Positions WHERE Title = N'HR Admin');
DECLARE @EngPosId INT = (SELECT TOP 1 Id FROM Positions WHERE Title = N'Software Engineer');
DECLARE @MgrPosId INT = (SELECT TOP 1 Id FROM Positions WHERE Title = N'Team Manager');

IF NOT EXISTS (SELECT 1 FROM Employees WHERE Email = N'manager@hr.local')
BEGIN
    INSERT INTO Employees (EmployeeCode, FullName, Gender, DateOfBirth, Email, Phone, DepartmentId, PositionId, BranchId, ManagerId, ContractType, JoinDate, Status, EmergencyContact, EducationHistory, WorkExperience, BasicSalary)
    VALUES (N'MGR001', N'Demo Manager', N'Female', '1990-05-12', N'manager@hr.local', N'012000001', @OpsDeptId, @MgrPosId, @BranchId, NULL, N'Full-time', '2020-01-15', N'Active', N'N/A', N'BA Business', N'5 years ops', 900);
END

DECLARE @ManagerEmployeeId INT = (SELECT Id FROM Employees WHERE Email = N'manager@hr.local');

IF NOT EXISTS (SELECT 1 FROM Employees WHERE Email = N'employee@hr.local')
BEGIN
    INSERT INTO Employees (EmployeeCode, FullName, Gender, DateOfBirth, Email, Phone, DepartmentId, PositionId, BranchId, ManagerId, ContractType, JoinDate, Status, EmergencyContact, EducationHistory, WorkExperience, BasicSalary)
    VALUES (N'EMP001', N'Demo Employee', N'Male', '1995-08-20', N'employee@hr.local', N'012000002', @EngDeptId, @EngPosId, @BranchId, @ManagerEmployeeId, N'Full-time', '2022-03-01', N'Active', N'N/A', N'BS Computer Science', N'2 years', 500);
END

IF NOT EXISTS (SELECT 1 FROM Employees WHERE Email = N'admin@hr.local')
BEGIN
    INSERT INTO Employees (EmployeeCode, FullName, Gender, DateOfBirth, Email, Phone, DepartmentId, PositionId, BranchId, ManagerId, ContractType, JoinDate, Status, EmergencyContact, EducationHistory, WorkExperience, BasicSalary)
    VALUES (N'HR001', N'Demo HR Admin', N'Female', '1988-01-08', N'admin@hr.local', N'012000003', @HrDeptId, @HrPosId, @BranchId, NULL, N'Full-time', '2019-06-01', N'Active', N'N/A', N'MBA HR', N'8 years HR', 1200);
END

DECLARE @EmployeeId INT = (SELECT Id FROM Employees WHERE Email = N'employee@hr.local');
DECLARE @AdminEmployeeId INT = (SELECT Id FROM Employees WHERE Email = N'admin@hr.local');

-- Password hashes for Employee@123 / Manager@123 / Admin@123 (PBKDF2 via app hasher)
IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = N'employee@hr.local')
BEGIN
    INSERT INTO Users (EmployeeId, RoleId, Email, PasswordHash, IsActive, MustChangePassword)
    VALUES (@EmployeeId, @EmployeeRoleId, N'employee@hr.local',
            N'pbkdf2$100000$fJIbCzXWsDRv4H6SHo45dQ==$J5VLitjceoOpIiRleg2bWN7CfCnqKs2u5BUVLHxaubQ=',
            1, 0);
END

IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = N'manager@hr.local')
BEGIN
    INSERT INTO Users (EmployeeId, RoleId, Email, PasswordHash, IsActive, MustChangePassword)
    VALUES (@ManagerEmployeeId, @ManagerRoleId, N'manager@hr.local',
            N'pbkdf2$100000$k/qqFfhELXuRiRrwVHtQ3Q==$ECY393H4qJmMUgboO4jJX1TjLtaC3M1421PmWLRhyB8=',
            1, 0);
END

IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = N'admin@hr.local')
BEGIN
    INSERT INTO Users (EmployeeId, RoleId, Email, PasswordHash, IsActive, MustChangePassword)
    VALUES (@AdminEmployeeId, @HrAdminRoleId, N'admin@hr.local',
            N'pbkdf2$100000$D1JxU+egoDP/hEBMHpl4qw==$NyaGNblD46GIXZI/yCJ9y7VKPgx+NbBIeDwyrLUMVX8=',
            1, 0);
END

PRINT 'Seed completed.';
