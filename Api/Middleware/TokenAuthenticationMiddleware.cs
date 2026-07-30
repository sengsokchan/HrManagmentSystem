using HrManagementSystem.Application;

namespace HrManagementSystem.Api.Middleware;

public sealed class TokenAuthenticationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ITokenService tokenService)
    {
        if (context.Request.Path.StartsWithSegments("/api") &&
            !context.Request.Path.StartsWithSegments("/api/auth/login") &&
            !context.Request.Path.StartsWithSegments("/api/auth/forgot-password"))
        {
            var authorization = context.Request.Headers.Authorization.ToString();

            if (authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) &&
                tokenService.TryValidate(authorization["Bearer ".Length..].Trim(), out var user))
            {
                context.Items["CurrentUser"] = user;
            }
        }

        await next(context);
    }
}
