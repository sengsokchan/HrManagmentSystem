using HrManagementSystem.Application;

namespace HrManagementSystem.Infrastructure;

public sealed class BusinessClock : IBusinessClock
{
    public DateTime LocalNow => DateTime.Now;
    public DateOnly Today => DateOnly.FromDateTime(LocalNow);
}
