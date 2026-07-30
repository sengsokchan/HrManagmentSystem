using HrManagementSystem.Application;
using Microsoft.AspNetCore.Mvc;

namespace HrManagementSystem.Api.Controllers;

[ApiController]
[Route("api/roles")]
public sealed class RolesController(IRoleService service) : ControllerBase
{
    [HttpGet]
    public IActionResult GetRoles()
    {
        var user = ControllerResult.RequireUser(this);
        if (user is null) return ControllerResult.UnauthorizedResult(this);
        if (!AuthorizationRules.Can(user, "roles.read")) return ControllerResult.Forbidden(this);

        return Ok(service.GetRoles());
    }
}
