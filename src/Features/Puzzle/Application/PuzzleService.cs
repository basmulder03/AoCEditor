using System.Globalization;
using Puzzle.Domain;
using Shared.Contracts;

namespace Puzzle.Application;

public enum FetchStatus { Ok, NotSignedIn, RateLimited, NotFound, NetworkError, Unreadable }

/// <summary>Reason is the gateway's network failure text, never a secret.</summary>
public sealed record Fetched<T>(FetchStatus Status, T? Value = default, string? Reason = null);

/// <summary>Unlocked is the refreshed puzzle after a correct part 1, null otherwise or when that refresh failed.</summary>
public sealed record Submission(SubmitResult Result, PuzzleDocument? Unlocked);

/// <summary>Puzzle text, input and answers for one day, each only on explicit user action.</summary>
public sealed class PuzzleService(IAocGateway gateway)
{
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

    /// <summary>Posts once and never retries. A correct part 1 is followed by one refresh of the puzzle page, the only automatic fetch, to pick up part 2.</summary>
    public async Task<Fetched<Submission>> SubmitAsync(int year, int day, int part, string answer, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(part, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(part, 2);
        ArgumentException.ThrowIfNullOrWhiteSpace(answer);
        var form = new Dictionary<string, string>
        {
            ["level"] = part.ToString(CultureInfo.InvariantCulture),
            ["answer"] = answer.Trim(),
        };
        var response = await gateway.PostFormAsync($"{DayPath(year, day)}/answer", form, cancellationToken);
        var submitted = Read(response, SubmitResponseParser.Parse);
        if (submitted.Value is not { } result)
        {
            return new Fetched<Submission>(submitted.Status, Reason: submitted.Reason);
        }

        var unlocked = result.Outcome == SubmitOutcome.Correct && part == 1
            ? (await LoadPuzzleAsync(year, day, refresh: true, cancellationToken)).Value
            : null;
        return new Fetched<Submission>(FetchStatus.Ok, new Submission(result, unlocked));
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
