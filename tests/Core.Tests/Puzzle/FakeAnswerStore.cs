using AoCEditor.Core.Puzzle.Domain;
using Shared.Contracts;

namespace AoCEditor.Core.Tests.Puzzle;

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
