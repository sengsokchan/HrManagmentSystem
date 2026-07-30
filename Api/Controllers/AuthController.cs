using HrManagementSystem.Application;
using Microsoft.AspNetCore.Mvc;

namespace HrManagementSystem.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService service) : ControllerBase
{
    [HttpPost("login")]
    public IActionResult Login(LoginRequest request)
    {
        var result = service.Login(request);
        return result.Status switch
        {
            LoginResultStatus.Success => Ok(result.Response),
            LoginResultStatus.TemporarilyLocked => StatusCode(
                StatusCodes.Status429TooManyRequests,
                new { message = result.Message, retryAt = result.RetryAt }),
            LoginResultStatus.AdminLocked => StatusCode(
                StatusCodes.Status423Locked,
                new { message = result.Message }),
            _ => Unauthorized(new { message = result.Message })
        };
    }

    [HttpPost("change-password")]
    public IActionResult ChangePassword(ChangePasswordRequest request)
    {
        var user = ControllerResult.RequireUser(this);
        if (user is null) return ControllerResult.UnauthorizedResult(this);

        try
        {
            service.ChangePassword(user, request);
            return Ok(new { message = "Password updated. You can continue using the app." });
        }
        catch (Exception exception)
        {
            return ControllerResult.Error(this, exception);
        }
    }

    [HttpPost("forgot-password")]
    public IActionResult ForgotPassword(ForgotPasswordRequest request)
    {
        try
        {
            return Ok(service.ForgotPassword(request));
        }
        catch (Exception exception)
        {
            return ControllerResult.Error(this, exception);
        }
    }

    [HttpGet("/api/me")]
    public IActionResult Me()
    {
        var user = ControllerResult.RequireUser(this);
        return user is null ? ControllerResult.UnauthorizedResult(this) : Ok(service.GetCurrentUser(user));
    }
}
