using HrManagementSystem.Application;
using Microsoft.AspNetCore.Mvc;

namespace HrManagementSystem.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
public sealed class DashboardController(IDashboardService service) : ControllerBase
{
    [HttpGet]
    public IActionResult GetDashboard()
    {
        var user = ControllerResult.RequireUser(this);
        return user is null ? ControllerResult.UnauthorizedResult(this) : Ok(service.GetDashboard());
    }
}
