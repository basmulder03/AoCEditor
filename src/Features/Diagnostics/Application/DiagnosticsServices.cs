using Diagnostics.Domain;
using Diagnostics.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Diagnostics.Application;

/// <summary>Wires the logger factory with the redaction layer and the file, buffer and console sinks.</summary>
public sealed class DiagnosticsServices : IDisposable
{
    public const long MaxFileBytes = 2 * 1024 * 1024;
    public const int MaxFiles = 5;
    public static readonly TimeSpan MaxAge = TimeSpan.FromDays(7);
    public const int BufferCapacity = 1000;
    public const int RequestCapacity = 100;

    private DiagnosticsServices(ILoggerFactory factory, Redactor redactor, DebugMode debug, LogBuffer buffer)
    {
        LoggerFactory = factory;
        Redactor = redactor;
        Debug = debug;
        Buffer = buffer;
    }

    public ILoggerFactory LoggerFactory { get; }

    public Redactor Redactor { get; }

    public DebugMode Debug { get; }

    public LogBuffer Buffer { get; }

    public RequestLog Requests { get; } = new(RequestCapacity);

    public static DiagnosticsServices Create(string root, DebugMode debug, string? homeDirectory, TimeProvider time, TextWriter? console = null)
    {
        var redactor = new Redactor(homeDirectory);
        var buffer = new LogBuffer(BufferCapacity);
        var sinks = new List<ILogSink> { buffer, new ConsoleLogSink(debug, console ?? Console.Error) };
        try
        {
            sinks.Add(new FileLogSink(Path.Combine(root, "logs"), time, MaxFileBytes, MaxFiles, MaxAge));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Logging to disk is optional; the buffer and console still work.
        }

        var factory = new LoggerFactory([new DiagnosticsLoggerProvider(redactor, debug, sinks, time)], new LoggerFilterOptions { MinLevel = LogLevel.Trace });
        return new DiagnosticsServices(factory, redactor, debug, buffer);
    }

    public void Dispose() => LoggerFactory.Dispose();
}
