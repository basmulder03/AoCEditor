using System.Globalization;
using Sync.Domain;

namespace Sync.UI;

/// <summary>One calendar day as shown in the grid; the state is always also spelled out in <see cref="Label"/>.</summary>
public sealed class DayTile
{
    public DayTile(int year, int day, int stars, DateTimeOffset now)
    {
        Year = year;
        Day = day;
        State = CalendarRules.StateOf(year, day, stars, now);
        Label = State switch
        {
            DayState.Locked => "Locked",
            DayState.Open => "Open",
            DayState.Part1Done => "One star",
            _ => "Two stars",
        };
        var unlock = CalendarRules.UnlockAt(year, day).UtcDateTime.ToString("MMM d 'at' HH:mm 'UTC'", CultureInfo.InvariantCulture);
        Name = State == DayState.Locked ? $"Day {day}, locked, unlocks {unlock}" : $"Day {day}, {Label.ToLowerInvariant()}";
    }

    public int Year { get; }

    public int Day { get; }

    public DayState State { get; }

    public string Label { get; }

    public string Name { get; }

    public string DayText => Day.ToString(CultureInfo.InvariantCulture);

    public bool IsLocked => State == DayState.Locked;

    public bool IsOpen => State == DayState.Open;

    public bool IsOneStar => State == DayState.Part1Done;

    public bool IsTwoStars => State == DayState.Complete;
}
