using Microsoft.Extensions.Logging;

namespace AoCEditor.Core.Diagnostics.Application;

/// <summary>Bounded in-memory log behind the debug view; the oldest entries fall off.</summary>
public sealed class BufferLoggerProvider(int capacity, TimeProvider time) : ILoggerProvider
{
    private readonly Queue<LogEntry> _entries = new();
    private readonly Lock _lock = new();
    private readonly TimeProvider _time = time;

    public event EventHandler? Changed;

    public ILogger CreateLogger(string categoryName) => new BufferLogger(this, LogEntry.SourceOf(categoryName));

    public IReadOnlyList<LogEntry> Snapshot()
    {
        lock (_lock)
        {
            return [.. _entries];
        }
    }

    public void Dispose()
    {
    }

    private void Add(LogEntry entry)
    {
        lock (_lock)
        {
            _entries.Enqueue(entry);
            while (_entries.Count > capacity)
            {
                _entries.Dequeue();
            }
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    private sealed class BufferLogger(BufferLoggerProvider owner, string source) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel))
            {
                owner.Add(new LogEntry(owner._time.GetUtcNow(), logLevel, source, formatter(state, exception)));
            }
        }
    }
}
