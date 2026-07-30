namespace HrManagementSystem.Application;

public static class LeaveCalculator
{
    public static readonly IReadOnlyDictionary<string, decimal> DefaultEntitlements =
        new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            ["Annual leave"] = 18m,
            ["Sick leave"] = 10m,
            ["Maternity leave"] = 90m,
            ["Emergency leave"] = 3m
        };

    public static bool TracksBalance(string leaveType) =>
        DefaultEntitlements.ContainsKey((leaveType ?? string.Empty).Trim());

    public static decimal CalculateDays(DateOnly start, DateOnly end, bool isHalfDay)
    {
        if (end < start)
        {
            throw new ArgumentException("End date must be on or after the start date.");
        }

        if (isHalfDay)
        {
            if (start != end)
            {
                throw new ArgumentException("Half-day leave must use the same start and end date.");
            }

            return 0.5m;
        }

        return end.DayNumber - start.DayNumber + 1;
    }

    /// <summary>Non-throwing day count for list/report display of existing rows.</summary>
    public static decimal DisplayDays(DateOnly start, DateOnly end, bool isHalfDay)
    {
        if (end < start) return 0m;
        if (isHalfDay) return 0.5m;
        return end.DayNumber - start.DayNumber + 1;
    }
}
