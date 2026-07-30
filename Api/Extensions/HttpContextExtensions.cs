using HrManagementSystem.Application;

namespace HrManagementSystem.Api;

public static class HttpContextExtensions
{
    public const string CurrentUserItemKey = "CurrentUser";

    public static UserContext? CurrentUser(this HttpContext context) =>
        context.Items.TryGetValue(CurrentUserItemKey, out var value) ? value as UserContext : null;
}
