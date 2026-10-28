using Microsoft.Extensions.Logging;

namespace AoCEditor.Core.Diagnostics.Application;

/// <summary>A log line after redaction; <see cref="Source"/> is the slice name.</summary>
public sealed record LogEntry(DateTimeOffset Time, LogLevel Level, string Source, string Message)
{
    /// <summary>The slice segment after the layer names the source; other categories keep their first segment.</summary>
    public static string SourceOf(string category)
    {
        var parts = category.Split('.');
        return parts.Length > 2 && parts[0] == "AoCEditor" && (parts[1] is "Core" or "Infrastructure" or "UI") ? parts[2] : parts[0];
    }
}
