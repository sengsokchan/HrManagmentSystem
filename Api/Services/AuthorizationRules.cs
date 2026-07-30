namespace HrManagementSystem.Application;

public static class AuthorizationRules
{
    public static bool Can(UserContext user, string permission) =>
        user.Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
}
