using HrManagementSystem.Infrastructure.Security;

namespace HrManagementSystem.Application.Services;

public sealed class EmployeeService(IHrRepository repository, IPasswordHasher passwords) : IEmployeeService
{
    public PagedResult<EmployeeView> GetEmployees(UserContext user, ListQuery query)
    {
        if (!AuthorizationRules.Can(user, "employees.read")) throw new UnauthorizedAccessException();

        var search = query.Search?.Trim() ?? string.Empty;
        var status = query.Status?.Trim() ?? string.Empty;

        var views = repository.Employees
            .OrderByDescending(employee => employee.Id)
            .Select(employee => employee.ToEmployeeView(repository));

        return Paging.Apply(views, query, employee =>
        {
            if (!string.IsNullOrWhiteSpace(status) &&
                !string.Equals(employee.Status.ToString(), status, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(search)) return true;

            var haystack = $"{employee.EmployeeCode} {employee.FullName} {employee.Department} {employee.Position} {employee.Branch} {employee.Email}";
            return haystack.Contains(search, StringComparison.OrdinalIgnoreCase);
        });
    }

    public EmployeeView? GetEmployee(int id, UserContext user)
    {
        if (!AuthorizationRules.Can(user, "employees.read")) throw new UnauthorizedAccessException();
        return repository.Employees.FirstOrDefault(e => e.Id == id)?.ToEmployeeView(repository);
    }

    public EmployeeCreateResult CreateEmployee(EmployeeWriteRequest request, UserContext user)
    {
        if (!AuthorizationRules.Can(user, "employees.write")) throw new UnauthorizedAccessException();

        var email = request.Email.Trim();
        if (repository.Users.Any(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("A login account with this email already exists.");
        }

        if (repository.Employees.Any(e => e.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("An employee with this email already exists.");
        }

        var employeeRole = repository.Roles.FirstOrDefault(r => r.Name.Equals("Employee", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("Employee role is not configured.");

        var temporaryPassword = PassphraseGenerator.Generate();
        var employee = repository.CreateEmployee(request);
        repository.CreateUserAccount(
            employee.Id,
            employeeRole.Id,
            email,
            passwords.Hash(temporaryPassword),
            mustChangePassword: true);

        repository.AddAudit(
            user.UserId,
            "Created",
            "Employee",
            employee.Id.ToString(),
            $"{employee.FullName} (login account created; temporary password issued)");

        return new EmployeeCreateResult(employee.ToEmployeeView(repository), email, temporaryPassword);
    }

    public EmployeeView? UpdateEmployee(int id, EmployeeWriteRequest request, UserContext user)
    {
        if (!AuthorizationRules.Can(user, "employees.write")) throw new UnauthorizedAccessException();
        var employee = repository.UpdateEmployee(id, request);
        if (employee is not null) repository.AddAudit(user.UserId, "Updated", "Employee", id.ToString(), employee.FullName);
        return employee?.ToEmployeeView(repository);
    }

    public bool DeactivateEmployee(int id, UserContext user)
    {
        if (!AuthorizationRules.Can(user, "employees.write")) throw new UnauthorizedAccessException();
        var employee = repository.DeactivateEmployee(id);
        if (employee is not null) repository.AddAudit(user.UserId, "Deactivated", "Employee", id.ToString(), employee.FullName);
        return employee is not null;
    }

    public PasswordResetResult ResetPassword(int employeeId, UserContext user)
    {
        if (!AuthorizationRules.Can(user, "employees.write")) throw new UnauthorizedAccessException();

        var employee = repository.Employees.FirstOrDefault(e => e.Id == employeeId)
            ?? throw new KeyNotFoundException("Employee not found.");

        var account = repository.Users.FirstOrDefault(u => u.EmployeeId == employeeId && u.IsActive)
            ?? throw new InvalidOperationException("This employee does not have an active login account.");

        var temporaryPassword = PassphraseGenerator.Generate();
        repository.UpdateUserPassword(account.Id, passwords.Hash(temporaryPassword), mustChangePassword: true);
        repository.ClearLoginSecurity(account.Email);
        repository.AddAudit(
            user.UserId,
            "Reset password",
            "User",
            account.Id.ToString(),
            $"{employee.FullName} (temporary password issued by admin)");

        return new PasswordResetResult(employee.FullName, account.Email, temporaryPassword);
    }
}
