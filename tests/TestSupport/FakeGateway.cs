using Shared.Contracts;

namespace AoCEditor.TestSupport;

public sealed class FakeGateway : IAocGateway
{
    public GatewayResult Reply { get; set; } = new GatewayResult.NotFound();

    /// <summary>Answer to POSTs when different from <see cref="Reply"/>.</summary>
    public GatewayResult? PostReply { get; set; }

    /// <summary>Answer to GETs when different from <see cref="Reply"/> and the page map.</summary>
    public GatewayResult? GetReply { get; set; }

    /// <summary>Per-path answers to GETs; other paths get <see cref="Reply"/>.</summary>
    public Dictionary<string, GatewayResult> Pages { get; } = [];

    public List<(string Path, RequestKind Kind, FetchPolicy Policy)> Requests { get; } = [];

    public List<(string Path, IReadOnlyDictionary<string, string> Form)> Posts { get; } = [];

    public DateTimeOffset NextAllowed { get; set; }

    /// <summary>When set, requests wait on it, like a gateway waiting for its spacing.</summary>
    public bool Hold { get; set; }

    /// <summary>Verdict of <see cref="CheckTokenAsync"/> when <see cref="Answers"/> is empty.</summary>
    public GatewayResult Answer { get; set; } = new GatewayResult.Ok("page");

    public Queue<GatewayResult> Answers { get; } = new();

    public List<string> Checked { get; } = [];

    public DateTimeOffset NextAllowedAt(RequestKind kind) => NextAllowed;

    public async Task<GatewayResult> GetAsync(string path, RequestKind kind, FetchPolicy policy, CancellationToken cancellationToken)
    {
        Requests.Add((path, kind, policy));
        if (Hold)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }

        return GetReply ?? (Pages.TryGetValue(path, out var page) ? page : Reply);
    }

    public Task<GatewayResult> PostFormAsync(string path, IReadOnlyDictionary<string, string> form, CancellationToken cancellationToken)
    {
        Posts.Add((path, form));
        return Task.FromResult(PostReply ?? Reply);
    }

    public Task<GatewayResult> CheckTokenAsync(string token, CancellationToken cancellationToken)
    {
        Checked.Add(token);
        return Task.FromResult(Answers.TryDequeue(out var next) ? next : Answer);
    }
}
