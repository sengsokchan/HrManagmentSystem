using HrManagementSystem.Application;
using Microsoft.AspNetCore.Mvc;

namespace HrManagementSystem.Api.Controllers;

[ApiController]
[Route("api/attendance")]
public sealed class AttendanceController(IAttendanceService service) : ControllerBase
{
    [HttpGet]
    public IActionResult GetAttendance([FromQuery] ListQuery query)
    {
        var user = ControllerResult.RequireUser(this);
        if (user is null) return ControllerResult.UnauthorizedResult(this);
        return Ok(service.GetAttendance(user, query));
    }

    [HttpPost("check-in")]
    public IActionResult CheckIn(AttendanceCheckRequest request)
    {
        var user = ControllerResult.RequireUser(this);
        if (user is null) return ControllerResult.UnauthorizedResult(this);

        try
        {
            return Ok(service.CheckIn(request, user));
        }
        catch (Exception exception)
        {
            return ControllerResult.Error(this, exception);
        }
    }

    [HttpPost("check-out")]
    public IActionResult CheckOut(AttendanceCheckRequest request)
    {
        var user = ControllerResult.RequireUser(this);
        if (user is null) return ControllerResult.UnauthorizedResult(this);

        try
        {
            var attendance = service.CheckOut(request, user);
            return attendance is null
                ? ControllerResult.NotFoundResult(this, "No check-in record found for today.")
                : Ok(attendance);
        }
        catch (Exception exception)
        {
            return ControllerResult.Error(this, exception);
        }
    }
}
