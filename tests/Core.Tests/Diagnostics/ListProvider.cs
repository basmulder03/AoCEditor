using Microsoft.Extensions.Logging;

namespace AoCEditor.Core.Tests.Diagnostics;

internal sealed class ListProvider : ILoggerProvider
{
    public List<(LogLevel Level, string Message, Exception? Exception)> Entries { get; } = [];

    public ILogger CreateLogger(string categoryName) => new ListLogger(Entries);

    public void Dispose()
    {
    }

    private sealed class ListLogger(List<(LogLevel, string, Exception?)> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            entries.Add((logLevel, formatter(state, exception), exception));
    }
}
