using AoCEditor.Core.Diagnostics.Domain;
using Microsoft.Extensions.Logging;

namespace AoCEditor.Core.Diagnostics.Application;

/// <summary>Wraps a provider so it only ever sees redacted text and a failing provider cannot break the caller.</summary>
public sealed class RedactingLoggerProvider(Redactor redactor, ILoggerProvider inner) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new RedactingLogger(redactor, inner.CreateLogger(categoryName));

    public void Dispose() => inner.Dispose();

    private sealed class RedactingLogger(Redactor redactor, ILogger inner) : ILogger
    {
        // Scope state is not formatted, so it could carry unredacted values.
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => inner.IsEnabled(logLevel);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!inner.IsEnabled(logLevel))
            {
                return;
            }

            var message = formatter(state, exception);
            var text = redactor.Apply(exception is null ? message : $"{message}{Environment.NewLine}{exception}");
            try
            {
                inner.Log(logLevel, eventId, text, null, static (s, _) => s);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or ObjectDisposedException)
            {
                // A broken provider (full disk) must never break the app; the others still get the entry.
            }
        }
    }
}
