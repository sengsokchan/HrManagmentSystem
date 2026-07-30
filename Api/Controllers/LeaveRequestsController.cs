using HrManagementSystem.Application;
using Microsoft.AspNetCore.Mvc;

namespace HrManagementSystem.Api.Controllers;

[ApiController]
[Route("api/leave-requests")]
public sealed class LeaveRequestsController(ILeaveService service) : ControllerBase
{
    [HttpGet]
    public IActionResult GetLeaveRequests([FromQuery] ListQuery query)
    {
        var user = ControllerResult.RequireUser(this);
        if (user is null) return ControllerResult.UnauthorizedResult(this);
        return Ok(service.GetLeaveRequests(user, query));
    }

    [HttpPost]
    public IActionResult CreateLeaveRequest(LeaveCreateRequest request)
    {
        var user = ControllerResult.RequireUser(this);
        if (user is null) return ControllerResult.UnauthorizedResult(this);

        try
        {
            var leave = service.CreateLeaveRequest(request, user);
            return Created($"/api/leave-requests/{leave.Id}", leave);
        }
        catch (Exception exception)
        {
            return ControllerResult.Error(this, exception);
        }
    }

    [HttpPut("{id:int}/decision")]
    public IActionResult DecideLeave(int id, LeaveDecisionRequest request)
    {
        var user = ControllerResult.RequireUser(this);
        if (user is null) return ControllerResult.UnauthorizedResult(this);

        try
        {
            var leave = service.DecideLeave(id, request, user);
            return leave is null ? ControllerResult.NotFoundResult(this, "Leave request not found.") : Ok(leave);
        }
        catch (Exception exception)
        {
            return ControllerResult.Error(this, exception);
        }
    }
}
