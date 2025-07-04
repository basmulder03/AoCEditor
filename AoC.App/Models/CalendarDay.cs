namespace AoC.App.Models;

public class CalendarDay
{
    public int Day { get; init; }

    public string DisplayText => $"Day {Day:00}";

    public static int LatestAvailableYear => AvailableYears.Max();

    public static IEnumerable<int> AvailableYears
    {
        get
        {
            var currentYear = DateTime.Now.Year;
            var range = IsDayAvailable(currentYear, 1)
                ? currentYear - Constants.EarliestAvailableYear + 1
                : currentYear - Constants.EarliestAvailableYear;

            return Enumerable.Range(Constants.EarliestAvailableYear, range);
        }
    }

    public static bool IsDayAvailable(int year, int day) =>
        DateTime.UtcNow >= new DateTime(year, 12, day, 5, 0, 0, DateTimeKind.Utc);
}