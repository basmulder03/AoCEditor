using Shared.Contracts;

namespace AoCEditor.Core.Tests.Sync;

internal static class Html
{
    public static string Events(params string[] hrefs) =>
        "<article>" + string.Concat(hrefs.Select(h => $"<div class=\"eventlist-event\"><a href=\"{h}\">[x]</a> <span class=\"star-count\">1*</span></div>")) + "</article>";

    /// <summary>Calendar with a day link per entry; stars 0, 1 or 2 map to the class names AoC uses.</summary>
    public static string Calendar(int year, params (int Day, int Stars)[] days) =>
        "<pre class=\"calendar\">" + string.Concat(days.Select(d =>
            $"<a aria-label=\"Day {d.Day}{Label(d.Stars)}\" href=\"/{year}/day/{d.Day}\" class=\"calendar-day{d.Day}{Marks(d.Stars)}\"><span class=\"calendar-day\">{d.Day}</span></a>\n")) + "</pre>";

    private static string Label(int stars) => stars switch
    {
        1 => ", one star",
        2 => ", two stars",
        _ => string.Empty,
    };

    private static string Marks(int stars) => stars switch
    {
        1 => " calendar-complete",
        2 => " calendar-verycomplete",
        _ => string.Empty,
    };
}
