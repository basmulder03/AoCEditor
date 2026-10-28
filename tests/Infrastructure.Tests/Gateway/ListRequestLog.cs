using System.Net;
using Gateway;
using Shared.Contracts;

namespace Gateway.Tests;

internal sealed class ListRequestLog : IRequestLog
{
    private readonly List<GatewayRequestEntry> _all = [];

    public IReadOnlyList<GatewayRequestEntry> Recent => _all;

    public event EventHandler? Changed;

    public void Record(GatewayRequestEntry entry)
    {
        _all.Add(entry);
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
