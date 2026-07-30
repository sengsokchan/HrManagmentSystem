using HrManagementSystem.Application;
using Microsoft.AspNetCore.Mvc;

namespace HrManagementSystem.Api.Controllers;

[ApiController]
[Route("api")]
public sealed class ReferenceDataController(IReferenceDataService service) : ControllerBase
{
    [HttpGet("departments")]
    public IActionResult GetDepartments() =>
        ControllerResult.RequireUser(this) is null ? ControllerResult.UnauthorizedResult(this) : Ok(service.GetDepartments());

    [HttpGet("positions")]
    public IActionResult GetPositions() =>
        ControllerResult.RequireUser(this) is null ? ControllerResult.UnauthorizedResult(this) : Ok(service.GetPositions());

    [HttpGet("branches")]
    public IActionResult GetBranches() =>
        ControllerResult.RequireUser(this) is null ? ControllerResult.UnauthorizedResult(this) : Ok(service.GetBranches());
}
