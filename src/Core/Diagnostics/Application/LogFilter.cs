using System.Globalization;
using Microsoft.Extensions.Logging;

namespace AoCEditor.Core.Diagnostics.Application;

/// <summary>Level and source filtering for the live log list.</summary>
public static class LogFilter
{
    public static IReadOnlyList<LogEntry> Apply(IEnumerable<LogEntry> entries, LogLevel? minimum, string? source) =>
        [.. entries
            .Where(e => (minimum is null || e.Level >= minimum) && (source is null || e.Source == source))
            .Reverse()];

    public static IReadOnlyList<string> Sources(IEnumerable<LogEntry> entries) =>
        [.. entries.Select(e => e.Source).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];

    public static string Line(LogEntry e) =>
        string.Create(CultureInfo.InvariantCulture, $"{e.Time:HH:mm:ss.fff} {e.Level} {e.Source} {e.Message}");
}
