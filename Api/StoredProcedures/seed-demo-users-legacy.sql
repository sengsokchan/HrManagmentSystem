-- Demo users for EmployeeManagementSystem (legacy Username/RoleName schema)
SET NOCOUNT ON;

IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleName = N'Manager')
  INSERT INTO Roles (RoleName) VALUES (N'Manager');
IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleName = N'Employee')
  INSERT INTO Roles (RoleName) VALUES (N'Employee');

IF NOT EXISTS (SELECT 1 FROM Positions WHERE PositionName = N'Manager')
  INSERT INTO Positions (PositionName, Level) VALUES (N'Manager', 2);
IF NOT EXISTS (SELECT 1 FROM Positions WHERE PositionName = N'Employee')
  INSERT INTO Positions (PositionName, Level) VALUES (N'Employee', 1);

DECLARE @HrAdminRoleId INT = (SELECT Id FROM Roles WHERE RoleName = N'HR Admin');
DECLARE @ManagerRoleId INT = (SELECT Id FROM Roles WHERE RoleName = N'Manager');
DECLARE @EmployeeRoleId INT = (SELECT Id FROM Roles WHERE RoleName = N'Employee');
DECLARE @DeptId INT = (SELECT TOP 1 Id FROM Departments);
DECLARE @BranchId INT = (SELECT TOP 1 Id FROM Branches);
DECLARE @ManagerPosId INT = (SELECT Id FROM Positions WHERE PositionName = N'Manager');
DECLARE @EmployeePosId INT = (SELECT Id FROM Positions WHERE PositionName = N'Employee');
DECLARE @HrPosId INT = (SELECT TOP 1 Id FROM Positions WHERE PositionName = N'HR Admin');

IF NOT EXISTS (SELECT 1 FROM Employees WHERE Email = N'manager@hr.local')
  INSERT INTO Employees (FullName, Gender, DateOfBirth, Phone, Email, JoinDate, Status, DepartmentId, PositionId, BranchId, ManagerId, BasicSalary)
  VALUES (N'Demo Manager', N'Male', '1990-05-12', N'012000001', N'manager@hr.local', '2020-01-15', N'Active', @DeptId, @ManagerPosId, @BranchId, NULL, 800);

DECLARE @ManagerEmployeeId INT = (SELECT Id FROM Employees WHERE Email = N'manager@hr.local');

IF NOT EXISTS (SELECT 1 FROM Employees WHERE Email = N'employee@hr.local')
  INSERT INTO Employees (FullName, Gender, DateOfBirth, Phone, Email, JoinDate, Status, DepartmentId, PositionId, BranchId, ManagerId, BasicSalary)
  VALUES (N'Demo Employee', N'Male', '1995-08-20', N'012000002', N'employee@hr.local', '2022-03-01', N'Active', @DeptId, @EmployeePosId, @BranchId, @ManagerEmployeeId, 500);

UPDATE Employees
SET FullName = N'Demo HR Admin',
    DepartmentId = ISNULL(DepartmentId, @DeptId),
    PositionId = ISNULL(PositionId, @HrPosId),
    BranchId = ISNULL(BranchId, @BranchId),
    Status = N'Active'
WHERE Email = N'admin@hr.local';

DECLARE @EmployeeId INT = (SELECT Id FROM Employees WHERE Email = N'employee@hr.local');
DECLARE @AdminEmployeeId INT = (SELECT Id FROM Employees WHERE Email = N'admin@hr.local');

-- PBKDF2 hashes for Employee@123 / Manager@123 / Admin@123
MERGE Users AS target
USING (SELECT N'admin@hr.local' AS Username) AS source
ON target.Username = source.Username
WHEN MATCHED THEN
  UPDATE SET EmployeeId = @AdminEmployeeId,
             RoleId = @HrAdminRoleId,
             IsActive = 1,
             MustChangePassword = 0,
             PasswordHash = N'pbkdf2$100000$D1JxU+egoDP/hEBMHpl4qw==$NyaGNblD46GIXZI/yCJ9y7VKPgx+NbBIeDwyrLUMVX8='
WHEN NOT MATCHED THEN
  INSERT (EmployeeId, Username, PasswordHash, RoleId, IsActive, MustChangePassword)
  VALUES (@AdminEmployeeId, N'admin@hr.local',
          N'pbkdf2$100000$D1JxU+egoDP/hEBMHpl4qw==$NyaGNblD46GIXZI/yCJ9y7VKPgx+NbBIeDwyrLUMVX8=',
          @HrAdminRoleId, 1, 0);

MERGE Users AS target
USING (SELECT N'manager@hr.local' AS Username) AS source
ON target.Username = source.Username
WHEN MATCHED THEN
  UPDATE SET EmployeeId = @ManagerEmployeeId,
             RoleId = @ManagerRoleId,
             IsActive = 1,
             MustChangePassword = 0,
             PasswordHash = N'pbkdf2$100000$k/qqFfhELXuRiRrwVHtQ3Q==$ECY393H4qJmMUgboO4jJX1TjLtaC3M1421PmWLRhyB8='
WHEN NOT MATCHED THEN
  INSERT (EmployeeId, Username, PasswordHash, RoleId, IsActive, MustChangePassword)
  VALUES (@ManagerEmployeeId, N'manager@hr.local',
          N'pbkdf2$100000$k/qqFfhELXuRiRrwVHtQ3Q==$ECY393H4qJmMUgboO4jJX1TjLtaC3M1421PmWLRhyB8=',
          @ManagerRoleId, 1, 0);

MERGE Users AS target
USING (SELECT N'employee@hr.local' AS Username) AS source
ON target.Username = source.Username
WHEN MATCHED THEN
  UPDATE SET EmployeeId = @EmployeeId,
             RoleId = @EmployeeRoleId,
             IsActive = 1,
             MustChangePassword = 0,
             PasswordHash = N'pbkdf2$100000$fJIbCzXWsDRv4H6SHo45dQ==$J5VLitjceoOpIiRleg2bWN7CfCnqKs2u5BUVLHxaubQ='
WHEN NOT MATCHED THEN
  INSERT (EmployeeId, Username, PasswordHash, RoleId, IsActive, MustChangePassword)
  VALUES (@EmployeeId, N'employee@hr.local',
          N'pbkdf2$100000$fJIbCzXWsDRv4H6SHo45dQ==$J5VLitjceoOpIiRleg2bWN7CfCnqKs2u5BUVLHxaubQ=',
          @EmployeeRoleId, 1, 0);

SELECT u.Username, r.RoleName, e.FullName, LEFT(u.PasswordHash, 40) AS HashPrefix
FROM Users u
LEFT JOIN Roles r ON r.Id = u.RoleId
LEFT JOIN Employees e ON e.Id = u.EmployeeId
WHERE u.Username IN (N'admin@hr.local', N'manager@hr.local', N'employee@hr.local')
ORDER BY u.Username;
