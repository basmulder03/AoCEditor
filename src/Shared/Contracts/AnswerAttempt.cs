namespace Shared.Contracts;

/// <summary>One submitted answer; Outcome is the name of the parsed result.</summary>
public sealed record AnswerAttempt(string Answer, string Outcome, DateTimeOffset At);
