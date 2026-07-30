using HrManagementSystem.Application;
using Microsoft.AspNetCore.Mvc;

namespace HrManagementSystem.Api.Controllers;

[ApiController]
[Route("api/employees")]
public sealed class EmployeesController(IEmployeeService service) : ControllerBase
{
    [HttpGet]
    public IActionResult GetEmployees([FromQuery] ListQuery query)
    {
        var user = ControllerResult.RequireUser(this);
        if (user is null) return ControllerResult.UnauthorizedResult(this);

        try
        {
            return Ok(service.GetEmployees(user, query));
        }
        catch (Exception exception)
        {
            return ControllerResult.Error(this, exception);
        }
    }

    [HttpGet("{id:int}")]
    public IActionResult GetEmployee(int id)
    {
        var user = ControllerResult.RequireUser(this);
        if (user is null) return ControllerResult.UnauthorizedResult(this);

        try
        {
            var employee = service.GetEmployee(id, user);
            return employee is null ? ControllerResult.NotFoundResult(this, "Employee not found.") : Ok(employee);
        }
        catch (Exception exception)
        {
            return ControllerResult.Error(this, exception);
        }
    }

    [HttpPost]
    public IActionResult CreateEmployee(EmployeeWriteRequest request)
    {
        var user = ControllerResult.RequireUser(this);
        if (user is null) return ControllerResult.UnauthorizedResult(this);
        if (!AuthorizationRules.Can(user, "employees.write")) return ControllerResult.Forbidden(this);

        try
        {
            var created = service.CreateEmployee(request, user);
            return Created($"/api/employees/{created.Employee.Id}", created);
        }
        catch (Exception exception)
        {
            return ControllerResult.Error(this, exception);
        }
    }

    [HttpPut("{id:int}")]
    public IActionResult UpdateEmployee(int id, EmployeeWriteRequest request)
    {
        var user = ControllerResult.RequireUser(this);
        if (user is null) return ControllerResult.UnauthorizedResult(this);
        if (!AuthorizationRules.Can(user, "employees.write")) return ControllerResult.Forbidden(this);

        try
        {
            var employee = service.UpdateEmployee(id, request, user);
            return employee is null ? ControllerResult.NotFoundResult(this, "Employee not found.") : Ok(employee);
        }
        catch (Exception exception)
        {
            return ControllerResult.Error(this, exception);
        }
    }

    [HttpDelete("{id:int}")]
    public IActionResult DeactivateEmployee(int id)
    {
        var user = ControllerResult.RequireUser(this);
        if (user is null) return ControllerResult.UnauthorizedResult(this);
        if (!AuthorizationRules.Can(user, "employees.write")) return ControllerResult.Forbidden(this);

        try
        {
            return service.DeactivateEmployee(id, user)
                ? NoContent()
                : ControllerResult.NotFoundResult(this, "Employee not found.");
        }
        catch (Exception exception)
        {
            return ControllerResult.Error(this, exception);
        }
    }

    [HttpPost("{id:int}/reset-password")]
    public IActionResult ResetPassword(int id)
    {
        var user = ControllerResult.RequireUser(this);
        if (user is null) return ControllerResult.UnauthorizedResult(this);
        if (!AuthorizationRules.Can(user, "employees.write")) return ControllerResult.Forbidden(this);

        try
        {
            return Ok(service.ResetPassword(id, user));
        }
        catch (Exception exception)
        {
            return ControllerResult.Error(this, exception);
        }
    }
}
