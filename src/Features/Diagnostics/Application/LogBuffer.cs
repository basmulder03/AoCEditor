namespace Diagnostics.Application;

/// <summary>Bounded in-memory sink behind the debug view; the oldest entries fall off.</summary>
public sealed class LogBuffer(int capacity) : ILogSink
{
    private readonly Queue<LogEntry> _entries = new();
    private readonly Lock _lock = new();

    public event EventHandler? Changed;

    public void Write(LogEntry entry)
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

    public IReadOnlyList<LogEntry> Snapshot()
    {
        lock (_lock)
        {
            return [.. _entries];
        }
    }
}
