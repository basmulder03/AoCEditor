using Puzzle.Domain;
using Shared.Contracts;

namespace Puzzle.Tests;

internal sealed class FakeGateway : IAocGateway
{
    public GatewayResult Reply { get; set; } = new GatewayResult.NotFound();

    /// <summary>Answer to POSTs when different from <see cref="Reply"/>.</summary>
    public GatewayResult? PostReply { get; set; }

    /// <summary>Answer to GETs when different from <see cref="Reply"/>.</summary>
    public GatewayResult? GetReply { get; set; }

    public List<(string Path, RequestKind Kind, FetchPolicy Policy)> Gets { get; } = [];

    public List<(string Path, IReadOnlyDictionary<string, string> Form)> Posts { get; } = [];

    public Task<GatewayResult> GetAsync(string path, RequestKind kind, FetchPolicy policy, CancellationToken cancellationToken)
    {
        Gets.Add((path, kind, policy));
        return Task.FromResult(GetReply ?? Reply);
    }

    public Task<GatewayResult> PostFormAsync(string path, IReadOnlyDictionary<string, string> form, CancellationToken cancellationToken)
    {
        Posts.Add((path, form));
        return Task.FromResult(PostReply ?? Reply);
    }

    public Task<GatewayResult> CheckTokenAsync(string token, CancellationToken cancellationToken) => Task.FromResult(Reply);

    public DateTimeOffset NextAllowedAt(RequestKind kind) => DateTimeOffset.MinValue;
}
