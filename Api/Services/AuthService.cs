using HrManagementSystem.Application;
using HrManagementSystem.Domain;
using HrManagementSystem.Infrastructure.Security;

namespace HrManagementSystem.Application.Services;

public sealed class AuthService(IHrRepository repository, IPasswordHasher passwords, ITokenService tokens) : IAuthService
{
    private const int MaxFailedAttempts = 5;

    public LoginResult Login(LoginRequest request)
    {
        var email = request.Email.Trim();
        var security = repository.GetLoginSecurity(email);
        var now = DateTime.UtcNow;

        if (security.RequiresAdminReset)
        {
            return new LoginResult(LoginResultStatus.AdminLocked, null, "Too many failed sign-in attempts. Please contact HR Admin to unlock this account.", null);
        }

        if (security.LockoutEndAt is not null && security.LockoutEndAt > now)
        {
            return new LoginResult(LoginResultStatus.TemporarilyLocked, null, $"Too many wrong attempts. Try again after {security.LockoutEndAt.Value.ToLocalTime():HH:mm:ss}.", security.LockoutEndAt);
        }

        var user = repository.Users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase) && u.IsActive);
        if (user is null || !passwords.Verify(request.Password, user.PasswordHash))
        {
            var failedState = repository.RecordFailedLogin(email, user?.Id);
            if (failedState.RequiresAdminReset)
            {
                return new LoginResult(LoginResultStatus.AdminLocked, null, "Too many failed sign-in attempts. Please contact HR Admin to unlock this account.", null);
            }

            if (failedState.FailedAccessCount >= MaxFailedAttempts && failedState.LockoutEndAt is not null)
            {
                return new LoginResult(LoginResultStatus.TemporarilyLocked, null, $"Wrong password {MaxFailedAttempts} times. Sign-in is blocked for 30 seconds.", failedState.LockoutEndAt);
            }

            return new LoginResult(LoginResultStatus.InvalidCredentials, null, $"Invalid email or password. {MaxFailedAttempts - failedState.FailedAccessCount} attempt(s) remaining.", null);
        }

        var role = repository.Roles.Single(r => r.Id == user.RoleId);
        var permissions = repository.GetPermissions(role.Name);
        var employee = user.EmployeeId is null ? null : repository.Employees.FirstOrDefault(e => e.Id == user.EmployeeId);
        var token = tokens.CreateToken(user, role.Name, permissions);
        repository.ClearLoginSecurity(email);

        var signedIn = new SignedInUser(
            user.Id,
            user.Email,
            role.Name,
            user.EmployeeId,
            employee?.FullName,
            permissions,
            user.MustChangePassword);

        return new LoginResult(
            LoginResultStatus.Success,
            new LoginResponse(token, signedIn, user.MustChangePassword),
            user.MustChangePassword
                ? "Signed in. Please set a new passphrase before continuing."
                : "Signed in successfully.",
            null);
    }

    public CurrentUserView? GetCurrentUser(UserContext user)
    {
        var account = repository.Users.FirstOrDefault(u => u.Id == user.UserId);
        var employee = user.EmployeeId is null ? null : repository.Employees.FirstOrDefault(e => e.Id == user.EmployeeId);
        return new CurrentUserView(
            user.UserId,
            user.Email,
            user.Role,
            user.EmployeeId,
            employee?.FullName,
            user.Permissions,
            account?.MustChangePassword ?? false);
    }

    public void ChangePassword(UserContext user, ChangePasswordRequest request)
    {
        var account = repository.Users.FirstOrDefault(u => u.Id == user.UserId && u.IsActive)
            ?? throw new UnauthorizedAccessException();

        if (!passwords.Verify(request.CurrentPassword, account.PasswordHash))
        {
            throw new InvalidOperationException("Current password is incorrect.");
        }

        if (!PasswordPolicy.TryValidate(request.NewPassword, out var message))
        {
            throw new ArgumentException(message);
        }

        if (string.Equals(request.CurrentPassword.Trim(), request.NewPassword.Trim(), StringComparison.Ordinal))
        {
            throw new ArgumentException("New passphrase must be different from the temporary password.");
        }

        repository.UpdateUserPassword(account.Id, passwords.Hash(request.NewPassword.Trim()), mustChangePassword: false);
        repository.AddAudit(user.UserId, "Changed password", "User", account.Id.ToString(), "Password updated");
    }

    public PasswordResetResult ForgotPassword(ForgotPasswordRequest request)
    {
        var email = (request.Email ?? string.Empty).Trim();
        var employeeCode = (request.EmployeeCode ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(employeeCode))
        {
            throw new ArgumentException("Email and employee code are required.");
        }

        var user = repository.Users.FirstOrDefault(u =>
            u.Email.Equals(email, StringComparison.OrdinalIgnoreCase) && u.IsActive);
        var employee = user?.EmployeeId is null
            ? null
            : repository.Employees.FirstOrDefault(e => e.Id == user.EmployeeId);

        var matched =
            user is not null &&
            employee is not null &&
            employee.EmployeeCode.Equals(employeeCode, StringComparison.OrdinalIgnoreCase) &&
            employee.Email.Equals(email, StringComparison.OrdinalIgnoreCase);

        if (!matched)
        {
            repository.RecordFailedLogin(email, user?.Id);
            throw new InvalidOperationException("Email and employee code do not match an active account.");
        }

        var temporaryPassword = PassphraseGenerator.Generate();
        repository.UpdateUserPassword(user!.Id, passwords.Hash(temporaryPassword), mustChangePassword: true);
        repository.ClearLoginSecurity(email);
        repository.AddAudit(user.Id, "Forgot password reset", "User", user.Id.ToString(), "Self-service temporary password issued");

        return new PasswordResetResult(employee!.FullName, user.Email, temporaryPassword);
    }
}
