using System.Globalization;
using Puzzle.Domain;
using Shared.Contracts;

namespace Puzzle.Application;

/// <summary>Puzzle text, input and answers for one day, each only on explicit user action.</summary>
public sealed class PuzzleService(IAocGateway gateway, IAnswerStore answers, TimeProvider time)
{
    private static readonly TimeSpan UnknownWait = TimeSpan.FromMinutes(1);

    public async Task<Fetched<PuzzleDocument>> LoadPuzzleAsync(int year, int day, bool refresh, CancellationToken cancellationToken)
    {
        var page = await gateway.GetAsync(DayPath(year, day), RequestKind.Interactive, Policy(refresh), cancellationToken);
        return Read(page, html => PuzzleParser.Parse(html));
    }

    public async Task<Fetched<string>> LoadInputAsync(int year, int day, bool refresh, CancellationToken cancellationToken)
    {
        var page = await gateway.GetAsync($"{DayPath(year, day)}/input", RequestKind.Interactive, Policy(refresh), cancellationToken);
        return Read(page, text => text);
    }

    /// <summary>The end of an active cooldown for this part, or null when a submit may go out.</summary>
    public async Task<DateTimeOffset?> CooldownUntilAsync(int year, int day, int part, CancellationToken cancellationToken) =>
        await answers.CooldownUntilAsync(year, day, part, cancellationToken) is { } until && until > time.GetUtcNow() ? until : null;

    /// <summary>Posts once and never retries. A correct part 1 is followed by one refresh of the puzzle page, the only automatic fetch, to pick up part 2.</summary>
    public async Task<Fetched<Submission>> SubmitAsync(int year, int day, int part, string answer, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(part, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(part, 2);
        ArgumentException.ThrowIfNullOrWhiteSpace(answer);
        answer = answer.Trim();
        if (await CooldownUntilAsync(year, day, part, cancellationToken) is not null)
        {
            return new Fetched<Submission>(FetchStatus.CoolingDown);
        }

        var tried = await answers.AttemptsAsync(year, day, part, cancellationToken);
        if (tried.Any(a => a.Answer == answer && a.Outcome is nameof(SubmitOutcome.Wrong) or nameof(SubmitOutcome.TooHigh) or nameof(SubmitOutcome.TooLow)))
        {
            return new Fetched<Submission>(FetchStatus.AlreadyTried);
        }

        var form = new Dictionary<string, string>
        {
            ["level"] = part.ToString(CultureInfo.InvariantCulture),
            ["answer"] = answer,
        };
        var response = await gateway.PostFormAsync($"{DayPath(year, day)}/answer", form, cancellationToken);
        var submitted = Read(response, SubmitResponseParser.Parse);
        if (submitted.Value is not { } result)
        {
            return new Fetched<Submission>(submitted.Status, Reason: submitted.Reason);
        }

        await RecordAsync(year, day, part, answer, result, cancellationToken);
        var unlocked = result.Outcome == SubmitOutcome.Correct && part == 1
            ? (await LoadPuzzleAsync(year, day, refresh: true, cancellationToken)).Value
            : null;
        return new Fetched<Submission>(FetchStatus.Ok, new Submission(result, unlocked));
    }

    private async Task RecordAsync(int year, int day, int part, string answer, SubmitResult result, CancellationToken cancellationToken)
    {
        var now = time.GetUtcNow();
        if (result.Outcome != SubmitOutcome.Unknown)
        {
            await answers.AddAttemptAsync(year, day, part, new AnswerAttempt(answer, result.Outcome.ToString(), now), cancellationToken);
        }

        // An unreadable wait or reply must not let the next press of Enter hit AoC again at once.
        var wait = result.Outcome switch
        {
            SubmitOutcome.RateLimited => result.Wait ?? UnknownWait,
            SubmitOutcome.Unknown => UnknownWait,
            _ => (TimeSpan?)null,
        };
        if (wait is { } w)
        {
            await answers.SetCooldownAsync(year, day, part, now + w, cancellationToken);
        }

        if (result.Outcome == SubmitOutcome.Correct)
        {
            await answers.SetKnownAnswerAsync(year, day, part, answer, cancellationToken);
        }
    }

    private static string DayPath(int year, int day) =>
        string.Create(CultureInfo.InvariantCulture, $"/{year}/day/{day}");

    private static FetchPolicy Policy(bool refresh) => refresh ? FetchPolicy.Refresh : FetchPolicy.CacheFirst;

    private static Fetched<T> Read<T>(GatewayResult result, Func<string, T?> parse) => result switch
    {
        GatewayResult.Ok ok => parse(ok.Content) is { } value ? new Fetched<T>(FetchStatus.Ok, value) : new Fetched<T>(FetchStatus.Unreadable),
        GatewayResult.NotSignedIn => new Fetched<T>(FetchStatus.NotSignedIn),
        GatewayResult.RateLimited => new Fetched<T>(FetchStatus.RateLimited),
        GatewayResult.NotFound => new Fetched<T>(FetchStatus.NotFound),
        GatewayResult.NetworkError e => new Fetched<T>(FetchStatus.NetworkError, Reason: e.Reason),
        _ => new Fetched<T>(FetchStatus.NetworkError),
    };
}
