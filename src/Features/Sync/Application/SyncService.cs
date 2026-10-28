using Shared.Contracts;
using Sync.Domain;

namespace Sync.Application;

public enum SyncOutcome
{
    Completed,
    NotSignedIn,
    RateLimited,
    EventsUnavailable,
}

public enum SyncError
{
    ParseError,
    NotFound,
    NetworkError,
}

public sealed record YearError(int Year, SyncError Error);

public sealed record SyncProgress(int Year, int Done, int Total);

public sealed record SyncSummary(SyncOutcome Outcome, IReadOnlyList<int> YearsSynced, int DaysUpdated, IReadOnlyList<YearError> Errors);

/// <summary>Fetches the event list and each year's calendar on explicit user request, saving progress per year.</summary>
public sealed class SyncService(IAocGateway gateway, ISyncStateStore store, TimeProvider time)
{
    public async Task<SyncSummary> SyncAsync(IProgress<SyncProgress> progress, CancellationToken cancellationToken)
    {
        var years = new List<int>();
        var errors = new List<YearError>();
        var updated = 0;

        SyncSummary Summary(SyncOutcome outcome) => new(outcome, years, updated, errors);

        var events = await gateway.GetAsync("/events", FetchPolicy.Refresh, cancellationToken);
        if (events is not GatewayResult.Ok eventsPage)
        {
            return Summary(Abort(events) ?? SyncOutcome.EventsUnavailable);
        }

        if (CalendarParser.ParseEvents(eventsPage.Content, time.GetUtcNow()) is not { } eventYears)
        {
            return Summary(SyncOutcome.EventsUnavailable);
        }

        for (var i = 0; i < eventYears.Count; i++)
        {
            var year = eventYears[i];
            var page = await gateway.GetAsync($"/{year}", FetchPolicy.Refresh, cancellationToken);
            if (page is GatewayResult.Ok ok)
            {
                if (CalendarParser.ParseCalendar(ok.Content, year) is { } days)
                {
                    updated += await SaveAsync(year, days, cancellationToken);
                    years.Add(year);
                }
                else
                {
                    errors.Add(new YearError(year, SyncError.ParseError));
                }
            }
            else if (Abort(page) is { } outcome)
            {
                return Summary(outcome);
            }
            else
            {
                errors.Add(new YearError(year, page is GatewayResult.NotFound ? SyncError.NotFound : SyncError.NetworkError));
            }

            progress.Report(new SyncProgress(year, i + 1, eventYears.Count));
        }

        return Summary(SyncOutcome.Completed);
    }

    private static SyncOutcome? Abort(GatewayResult result) => result switch
    {
        GatewayResult.NotSignedIn => SyncOutcome.NotSignedIn,
        GatewayResult.RateLimited => SyncOutcome.RateLimited,
        _ => null,
    };

    private async Task<int> SaveAsync(int year, IReadOnlyList<SyncedDay> days, CancellationToken cancellationToken)
    {
        var before = (await store.ReadYearAsync(year, cancellationToken)).ToDictionary(d => d.Day, d => d.Stars);
        await store.ReplaceYearAsync(year, days, time.GetUtcNow(), cancellationToken);
        return days.Count(d => before.GetValueOrDefault(d.Day, -1) != d.Stars);
    }
}
