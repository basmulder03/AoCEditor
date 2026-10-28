using Shared.Contracts;

namespace AoCEditor.Core.Sync.Domain;

public static class CalendarRules
{
    private static readonly TimeSpan EventOffset = TimeSpan.FromHours(-5);

    /// <summary>Day N of an event unlocks at 00:00 UTC-5 on 1 December plus N-1 days; never requested from the server.</summary>
    public static DateTimeOffset UnlockAt(int year, int day) => new(year, 12, day, 0, 0, 0, EventOffset);

    public const int FirstEventYear = 2015;

    /// <summary>The newest event year: December opens the current year's event, any other month still shows last year's.</summary>
    public static int LatestEventYear(DateTimeOffset now)
    {
        var local = now.ToOffset(EventOffset);
        return local.Month == 12 ? local.Year : local.Year - 1;
    }

    /// <summary>Stars always win; without stars the clock decides between locked and open.</summary>
    public static DayState StateOf(int year, int day, int stars, DateTimeOffset now) => stars switch
    {
        >= 2 => DayState.Complete,
        1 => DayState.Part1Done,
        _ => now >= UnlockAt(year, day) ? DayState.Open : DayState.Locked,
    };
}
