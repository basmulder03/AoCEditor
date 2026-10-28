using System.Globalization;
using System.Text.RegularExpressions;
using Shared.Contracts;

namespace Sync.Domain;

/// <summary>Reads event years and day stars from AoC pages; returns null for markup it does not recognise.</summary>
public static partial class CalendarParser
{
    private const int MaxDay = 25;
    private static readonly TimeSpan EventOffset = TimeSpan.FromHours(-5);

    [GeneratedRegex(@"eventlist-event[^>]*>\s*<a\s[^>]*?href=""/(\d{4})?""[^>]*>", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex EventLink();

    [GeneratedRegex(@"<a\s[^>]*>", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex Anchor();

    [GeneratedRegex(@"href=""/(\d{4})/day/(\d{1,3})""", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex DayHref();

    [GeneratedRegex(@"class=""([^""]*)""", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex ClassAttribute();

    /// <summary>The newest event is linked as the home page, so its year comes from the clock.</summary>
    public static IReadOnlyList<int>? ParseEvents(string html, DateTimeOffset now)
    {
        var local = now.ToOffset(EventOffset);
        var latest = local.Month == 12 ? local.Year : local.Year - 1;
        var years = EventLink().Matches(html)
            .Select(m => m.Groups[1].Success ? int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture) : latest)
            .Distinct()
            .ToList();
        return years.Count == 0 ? null : years;
    }

    public static IReadOnlyList<SyncedDay>? ParseCalendar(string html, int year)
    {
        var days = new SortedDictionary<int, int>();
        foreach (Match anchor in Anchor().Matches(html))
        {
            var href = DayHref().Match(anchor.Value);
            if (!href.Success || int.Parse(href.Groups[1].Value, CultureInfo.InvariantCulture) != year)
            {
                continue;
            }

            var day = int.Parse(href.Groups[2].Value, CultureInfo.InvariantCulture);
            if (day is < 1 or > MaxDay || !days.TryAdd(day, StarsOf(anchor.Value)))
            {
                return null;
            }
        }

        return days.Count == 0 ? null : [.. days.Select(d => new SyncedDay(d.Key, d.Value))];
    }

    private static int StarsOf(string anchor)
    {
        var classes = ClassAttribute().Match(anchor).Groups[1].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return classes.Contains("calendar-verycomplete") ? 2 : classes.Contains("calendar-complete") ? 1 : 0;
    }
}
