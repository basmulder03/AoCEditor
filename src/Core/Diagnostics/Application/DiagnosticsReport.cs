using System.Globalization;
using System.Text;
using AoCEditor.Core.Diagnostics.Domain;
using Shared.Contracts;

namespace AoCEditor.Core.Diagnostics.Application;

/// <summary>Builds the text behind "Copy diagnostics"; the result is redacted.</summary>
public static class DiagnosticsReport
{
    public static string Build(DiagnosticsSnapshot s, Redactor redactor)
    {
        var text = new StringBuilder();
        text.AppendLine(CultureInfo.InvariantCulture, $"App: {s.AppVersion}");
        text.AppendLine(CultureInfo.InvariantCulture, $"OS: {s.Os}");
        text.AppendLine(CultureInfo.InvariantCulture, $"Runtime: {s.Runtime}");
        text.AppendLine(CultureInfo.InvariantCulture, $"Session: {s.SessionState}");
        text.AppendLine();
        text.AppendLine("Storage");
        text.AppendLine(CultureInfo.InvariantCulture, $"Database: {s.Storage.DatabasePath}");
        text.AppendLine(CultureInfo.InvariantCulture, $"Schema version: {s.Storage.SchemaVersion}");
        text.AppendLine(CultureInfo.InvariantCulture, $"Migrations applied: {s.Storage.MigrationsApplied}");
        if (s.Storage.Error is not null)
        {
            text.AppendLine(CultureInfo.InvariantCulture, $"Error: {s.Storage.Error}");
        }

        text.AppendLine();
        text.AppendLine("Recent requests");
        foreach (var r in s.Requests)
        {
            text.AppendLine(CultureInfo.InvariantCulture, $"{r.At:HH:mm:ss} {RequestLine(r)}");
        }

        text.AppendLine();
        text.AppendLine("Recent log");
        foreach (var e in s.Logs)
        {
            text.AppendLine(CultureInfo.InvariantCulture, $"{e.Time:HH:mm:ss.fff} {e.Level} {e.Source} {e.Message}");
        }

        return redactor.Apply(text.ToString());
    }

    public static string RequestLine(GatewayRequestEntry r) =>
        string.Create(CultureInfo.InvariantCulture, $"{r.Method} {r.Path} {(r.Status?.ToString(CultureInfo.InvariantCulture) ?? "-")} {(long)r.Duration.TotalMilliseconds} ms (throttle wait {(long)r.ThrottleWait.TotalMilliseconds} ms)");
}
