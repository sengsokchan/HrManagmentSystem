using HrManagementSystem.Api.Middleware;
using HrManagementSystem.Application;
using HrManagementSystem.Application.Services;
using HrManagementSystem.Infrastructure;
using HrManagementSystem.Infrastructure.Persistence;
using HrManagementSystem.Infrastructure.Security;

// Local SQL Server may negotiate TLS < 1.2; Encrypt=False + this switch removes the console warning.
AppContext.SetSwitch("Switch.Microsoft.Data.SqlClient.SuppressInsecureTLSWarning", true);

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://127.0.0.1:5088");

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
});

builder.Services.AddSingleton<IBusinessClock, BusinessClock>();
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddSingleton<IHrRepository, SqlHrRepository>();
builder.Services.AddSingleton<ILeaveDecisionRepository>(sp => (ILeaveDecisionRepository)sp.GetRequiredService<IHrRepository>());
builder.Services.AddSingleton<IAuthService, AuthService>();
builder.Services.AddSingleton<IEmployeeService, EmployeeService>();
builder.Services.AddSingleton<IAttendanceService, AttendanceService>();
builder.Services.AddSingleton<ILeaveService, LeaveService>();
builder.Services.AddSingleton<IPayrollService, PayrollService>();
builder.Services.AddSingleton<IDashboardService, DashboardService>();
builder.Services.AddSingleton<IRoleService, RoleService>();
builder.Services.AddSingleton<IReferenceDataService, ReferenceDataService>();

var app = builder.Build();

app.UseCors();
app.UseMiddleware<TokenAuthenticationMiddleware>();
app.MapControllers();

app.Run();
