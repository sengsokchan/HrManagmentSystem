using HrManagementSystem.Application;
using Microsoft.AspNetCore.Mvc;

namespace HrManagementSystem.Api.Controllers;

[ApiController]
[Route("api/leave-balances")]
public sealed class LeaveBalancesController(ILeaveService service) : ControllerBase
{
    [HttpGet]
    public IActionResult GetLeaveBalances([FromQuery] int? employeeId, [FromQuery] int? year)
    {
        var user = ControllerResult.RequireUser(this);
        if (user is null) return ControllerResult.UnauthorizedResult(this);

        try
        {
            return Ok(service.GetLeaveBalances(user, employeeId, year));
        }
        catch (Exception exception)
        {
            return ControllerResult.Error(this, exception);
        }
    }

    [HttpPut]
    public IActionResult UpsertLeaveBalance(LeaveBalanceUpsertRequest request)
    {
        var user = ControllerResult.RequireUser(this);
        if (user is null) return ControllerResult.UnauthorizedResult(this);

        try
        {
            return Ok(service.UpsertLeaveBalance(request, user));
        }
        catch (Exception exception)
        {
            return ControllerResult.Error(this, exception);
        }
    }
}
