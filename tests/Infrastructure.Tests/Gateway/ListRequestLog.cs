using System.Net;
using Shared.Contracts;

namespace AoCEditor.Infrastructure.Tests.Gateway;

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
