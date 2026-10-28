using Shared.Contracts;

namespace AoCEditor.Core.Tests.Sync;

internal sealed class FakeGateway : IAocGateway
{
    public Dictionary<string, GatewayResult> Pages { get; } = [];

    public List<(string Path, RequestKind Kind, FetchPolicy Policy)> Requests { get; } = [];

    public DateTimeOffset NextAllowed { get; set; }

    /// <summary>When set, requests wait on it, like a gateway waiting for its spacing.</summary>
    public bool Hold { get; set; }

    public DateTimeOffset NextAllowedAt(RequestKind kind) => NextAllowed;

    public async Task<GatewayResult> GetAsync(string path, RequestKind kind, FetchPolicy policy, CancellationToken cancellationToken)
    {
        Requests.Add((path, kind, policy));
        if (Hold)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }

        return Pages.TryGetValue(path, out var r) ? r : new GatewayResult.NotFound();
    }

    public Task<GatewayResult> PostFormAsync(string path, IReadOnlyDictionary<string, string> form, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<GatewayResult> CheckTokenAsync(string token, CancellationToken cancellationToken) =>
        throw new NotSupportedException();
}
