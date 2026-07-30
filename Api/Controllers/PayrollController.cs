using HrManagementSystem.Application;
using Microsoft.AspNetCore.Mvc;

namespace HrManagementSystem.Api.Controllers;

[ApiController]
[Route("api/payroll")]
public sealed class PayrollController(IPayrollService service) : ControllerBase
{
    [HttpGet]
    public IActionResult GetPayroll([FromQuery] ListQuery query)
    {
        var user = ControllerResult.RequireUser(this);
        if (user is null) return ControllerResult.UnauthorizedResult(this);

        try
        {
            return Ok(service.GetPayroll(user, query));
        }
        catch (Exception exception)
        {
            return ControllerResult.Error(this, exception);
        }
    }

    [HttpPost("generate")]
    public IActionResult GeneratePayroll(PayrollGenerateRequest request)
    {
        var user = ControllerResult.RequireUser(this);
        if (user is null) return ControllerResult.UnauthorizedResult(this);

        try
        {
            return Ok(service.GeneratePayroll(request, user));
        }
        catch (Exception exception)
        {
            return ControllerResult.Error(this, exception);
        }
    }

    [HttpPut("{id:int}/status")]
    public IActionResult UpdateStatus(int id, PayrollStatusUpdateRequest request)
    {
        var user = ControllerResult.RequireUser(this);
        if (user is null) return ControllerResult.UnauthorizedResult(this);

        try
        {
            var payroll = service.UpdatePayrollStatus(id, request, user);
            return payroll is null
                ? ControllerResult.NotFoundResult(this, "Payroll record not found.")
                : Ok(payroll);
        }
        catch (Exception exception)
        {
            return ControllerResult.Error(this, exception);
        }
    }
}
