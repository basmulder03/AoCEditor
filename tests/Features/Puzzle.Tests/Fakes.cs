using Puzzle.Domain;
using Shared.Contracts;

namespace Puzzle.Tests;

internal sealed class FakeAnswerStore : IAnswerStore
{
    public List<AnswerAttempt> Attempts { get; } = [];

    public Dictionary<(int, int, int), DateTimeOffset> Cooldowns { get; } = [];

    public Dictionary<(int, int, int), string> Known { get; } = [];

    public Task AddAttemptAsync(int year, int day, int part, AnswerAttempt attempt, CancellationToken cancellationToken)
    {
        Attempts.Add(attempt);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<AnswerAttempt>> AttemptsAsync(int year, int day, int part, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<AnswerAttempt>>(Attempts);

    public Task SetCooldownAsync(int year, int day, int part, DateTimeOffset until, CancellationToken cancellationToken)
    {
        Cooldowns[(year, day, part)] = until;
        return Task.CompletedTask;
    }

    public Task<DateTimeOffset?> CooldownUntilAsync(int year, int day, int part, CancellationToken cancellationToken) =>
        Task.FromResult(Cooldowns.TryGetValue((year, day, part), out var until) ? until : (DateTimeOffset?)null);

    public Task SetKnownAnswerAsync(int year, int day, int part, string answer, CancellationToken cancellationToken)
    {
        Known[(year, day, part)] = answer;
        return Task.CompletedTask;
    }

    public Task<string?> KnownAnswerAsync(int year, int day, int part, CancellationToken cancellationToken) =>
        Task.FromResult(Known.TryGetValue((year, day, part), out var answer) ? answer : null);
}

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
