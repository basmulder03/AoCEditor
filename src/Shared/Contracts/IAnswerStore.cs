namespace Shared.Contracts;

/// <summary>Submission history, cooldowns and known correct answers per day and part (1 or 2).</summary>
public interface IAnswerStore
{
    Task AddAttemptAsync(int year, int day, int part, AnswerAttempt attempt, CancellationToken cancellationToken);

    /// <summary>Oldest first.</summary>
    Task<IReadOnlyList<AnswerAttempt>> AttemptsAsync(int year, int day, int part, CancellationToken cancellationToken);

    Task SetCooldownAsync(int year, int day, int part, DateTimeOffset until, CancellationToken cancellationToken);

    /// <summary>The stored end time, which may already be in the past.</summary>
    Task<DateTimeOffset?> CooldownUntilAsync(int year, int day, int part, CancellationToken cancellationToken);

    Task SetKnownAnswerAsync(int year, int day, int part, string answer, CancellationToken cancellationToken);

    Task<string?> KnownAnswerAsync(int year, int day, int part, CancellationToken cancellationToken);
}
