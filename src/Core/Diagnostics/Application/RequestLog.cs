using Shared.Contracts;

namespace Diagnostics.Application;

public sealed class RequestLog(int capacity) : IRequestLog
{
    private readonly LinkedList<GatewayRequestEntry> _entries = new();
    private readonly Lock _lock = new();

    public event EventHandler? Changed;

    public IReadOnlyList<GatewayRequestEntry> Recent
    {
        get
        {
            lock (_lock)
            {
                return [.. _entries];
            }
        }
    }

    public void Record(GatewayRequestEntry entry)
    {
        lock (_lock)
        {
            _entries.AddFirst(entry);
            while (_entries.Count > capacity)
            {
                _entries.RemoveLast();
            }
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }
}
