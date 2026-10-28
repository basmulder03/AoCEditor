using Diagnostics.Domain;
using Microsoft.Extensions.Logging;

namespace Diagnostics.Application;

/// <summary>Formats, redacts and fans out every log call; this is the only place sinks are fed.</summary>
public sealed class DiagnosticsLoggerProvider(Redactor redactor, DebugMode debug, IEnumerable<ILogSink> sinks, TimeProvider time) : ILoggerProvider
{
    private readonly ILogSink[] _sinks = [.. sinks];

    public ILogger CreateLogger(string categoryName) => new SinkLogger(this, ShortSource(categoryName));

    public void Dispose()
    {
    }

    // "Session.Application.SignInCoordinator" -> "Session".
    private static string ShortSource(string category)
    {
        var dot = category.IndexOf('.', StringComparison.Ordinal);
        return dot > 0 ? category[..dot] : category;
    }

    private bool Enabled(LogLevel level) => level != LogLevel.None && level >= (debug.IsEnabled ? LogLevel.Debug : LogLevel.Information);

    private void Write(LogLevel level, string source, string message, Exception? exception)
    {
        var text = exception is null ? message : $"{message}{Environment.NewLine}{exception}";
        var entry = new LogEntry(time.GetUtcNow(), level, source, redactor.Apply(text));
        foreach (var sink in _sinks)
        {
            try
            {
                sink.Write(entry);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or ObjectDisposedException)
            {
                // A broken sink (full disk) must never break the app; the others still get the entry.
            }
        }
    }

    private sealed class SinkLogger(DiagnosticsLoggerProvider owner, string source) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => owner.Enabled(logLevel);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (owner.Enabled(logLevel))
            {
                owner.Write(logLevel, source, formatter(state, exception), exception);
            }
        }
    }
}
