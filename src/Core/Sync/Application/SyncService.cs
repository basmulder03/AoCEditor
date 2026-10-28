using AoCEditor.Core.Sync.Domain;
using Microsoft.Extensions.Logging;
using Shared.Contracts;

namespace AoCEditor.Core.Sync.Application;

/// <summary>Fetches the event list and calendar pages on explicit user request, saving progress per year.</summary>
public sealed class SyncService(IAocGateway gateway, ISyncStateStore store, TimeProvider time, ILogger<SyncService> log)
{
    private readonly ILogger _log = log;

    /// <summary>Syncs one year; an already synced year is left alone unless <paramref name="refresh"/> is set.</summary>
    public async Task<SyncSummary> SyncYearAsync(int year, bool refresh, IProgress<SyncProgress> progress, CancellationToken cancellationToken)
    {
        var run = new Run();
        var synced = await store.SyncedYearsAsync(cancellationToken);
        if (!refresh && synced.Contains(year))
        {
            run.Skipped.Add(year);
            _log.YearSkipped(year);
            return run.Summary(SyncOutcome.Completed);
        }

        return await RunAsync(run, [year], progress, cancellationToken);
    }

    /// <summary>Queues every listed year not yet synced, except those in <paramref name="refreshYears"/> which are fetched again. The gateway spaces the requests, so this can take minutes.</summary>
    public async Task<SyncSummary> SyncAllAsync(IReadOnlyCollection<int> refreshYears, IProgress<SyncProgress> progress, CancellationToken cancellationToken)
    {
        var run = new Run();
        _log.SyncAllStarted();
        var events = await gateway.GetAsync("/events", RequestKind.Statistics, FetchPolicy.Refresh, cancellationToken);
        if (events is not GatewayResult.Ok eventsPage)
        {
            _log.EventsUnavailable(Describe(events));
            return run.Summary(Abort(events) ?? SyncOutcome.EventsUnavailable);
        }

        if (CalendarParser.ParseEvents(eventsPage.Content, time.GetUtcNow()) is not { } eventYears)
        {
            _log.EventsUnreadable();
            return run.Summary(SyncOutcome.EventsUnavailable);
        }

        var synced = await store.SyncedYearsAsync(cancellationToken);
        var queue = new List<int>();
        foreach (var year in eventYears)
        {
            (synced.Contains(year) && !refreshYears.Contains(year) ? run.Skipped : queue).Add(year);
        }

        return await RunAsync(run, queue, progress, cancellationToken);
    }

    private async Task<SyncSummary> RunAsync(Run run, List<int> queue, IProgress<SyncProgress> progress, CancellationToken cancellationToken)
    {
        for (var i = 0; i < queue.Count; i++)
        {
            var year = queue[i];
            var wait = gateway.NextAllowedAt(RequestKind.Statistics) - time.GetUtcNow();
            progress.Report(new SyncProgress(year, queue.Count - i - 1, (int)Math.Ceiling(Math.Max(0, wait.TotalSeconds))));

            _log.YearStarted(year, queue.Count - i - 1);
            var page = await gateway.GetAsync($"/{year}", RequestKind.Statistics, FetchPolicy.Refresh, cancellationToken);
            if (page is GatewayResult.Ok ok)
            {
                if (CalendarParser.ParseCalendar(ok.Content, year) is { } days)
                {
                    run.Updated += await SaveAsync(year, days, cancellationToken);
                    run.Years.Add(year);
                    _log.YearSynced(year, days.Count);
                }
                else
                {
                    _log.YearUnreadable(year);
                    run.Errors.Add(new YearError(year, SyncError.ParseError));
                }
            }
            else if (Abort(page) is { } outcome)
            {
                _log.SyncAborted(year, outcome);
                return run.Summary(outcome);
            }
            else
            {
                _log.YearFailed(year, Describe(page));
                run.Errors.Add(new YearError(year, page is GatewayResult.NotFound ? SyncError.NotFound : SyncError.NetworkError, (page as GatewayResult.NetworkError)?.Reason));
            }
        }

        return run.Summary(SyncOutcome.Completed);
    }

    private sealed class Run
    {
        public List<int> Years { get; } = [];

        public List<int> Skipped { get; } = [];

        public List<YearError> Errors { get; } = [];

        public int Updated { get; set; }

        public SyncSummary Summary(SyncOutcome outcome) => new(outcome, Years, Updated, Errors, Skipped);
    }

    private static string Describe(GatewayResult result) =>
        result is GatewayResult.NetworkError { Reason: { } reason } ? $"NetworkError ({reason})" : result.GetType().Name;

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
