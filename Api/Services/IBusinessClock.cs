namespace HrManagementSystem.Application;

public interface IBusinessClock
{
    DateTime LocalNow { get; }
    DateOnly Today { get; }
}
