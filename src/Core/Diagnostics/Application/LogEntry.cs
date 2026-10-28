using Microsoft.Extensions.Logging;

namespace AoCEditor.Core.Diagnostics.Application;

/// <summary>A log line after redaction; <see cref="Source"/> is the slice name.</summary>
public sealed record LogEntry(DateTimeOffset Time, LogLevel Level, string Source, string Message);
