using HrManagementSystem.Application;
using Microsoft.AspNetCore.Mvc;

namespace HrManagementSystem.Api.Controllers;

internal static class ControllerResult
{
    public static UserContext? RequireUser(ControllerBase controller) => controller.HttpContext.CurrentUser();

    public static IActionResult UnauthorizedResult(ControllerBase controller) =>
        controller.Unauthorized(new { message = "Authentication required." });

    public static IActionResult Forbidden(ControllerBase controller) =>
        controller.StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not have permission to perform this action." });

    public static IActionResult NotFoundResult(ControllerBase controller, string? message = null) =>
        controller.NotFound(new { message = message ?? "Resource not found." });

    public static IActionResult Error(ControllerBase controller, Exception exception) =>
        exception switch
        {
            UnauthorizedAccessException => Forbidden(controller),
            KeyNotFoundException notFound => NotFoundResult(controller, notFound.Message),
            InvalidOperationException invalid => controller.BadRequest(new { message = invalid.Message }),
            ArgumentException argument => controller.BadRequest(new { message = argument.Message }),
            _ => controller.StatusCode(StatusCodes.Status500InternalServerError, new { message = exception.Message })
        };
}
