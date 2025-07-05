namespace AoC.Shared.Models;

public class CalendarDay
{
    public int Year { get; set; }
    public int Day { get; set; }
    public string DisplayName => $"Day {Day:D2}";
    public bool IsLocked { get; init; }
}