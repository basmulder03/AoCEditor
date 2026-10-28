using System.Globalization;
using AoCEditor.Core.Diagnostics.Application;

namespace AoCEditor.Infrastructure.Diagnostics;

/// <summary>Writes to the console while debug mode is on.</summary>
public sealed class ConsoleLogSink(DebugMode debug, TextWriter output) : ILogSink
{
    public void Write(LogEntry entry)
    {
        if (debug.IsEnabled)
        {
            output.WriteLine($"{entry.Time.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture)} {entry.Level} {entry.Source} {entry.Message}");
        }
    }
}
