using System.Runtime.CompilerServices;

namespace OfficePlanner;

public static class DateOnlyExtensions
{
    public static DateOnly GetNextWorkingDay(this DateOnly day)
    {
        DateOnly nextDay = day.AddDays(1);
        while (nextDay.DayOfWeek == DayOfWeek.Saturday || nextDay.DayOfWeek == DayOfWeek.Sunday)
        {
            nextDay = nextDay.AddDays(1);
        }
        return nextDay;
    }

    public static DateOnly GetPreviousWorkingDay(this DateOnly day)
    {
        DateOnly previousDay = day.AddDays(-1);
        while (previousDay.DayOfWeek == DayOfWeek.Saturday || previousDay.DayOfWeek == DayOfWeek.Sunday)
        {
            previousDay = previousDay.AddDays(-1);
        }
        return previousDay;
    }
}
