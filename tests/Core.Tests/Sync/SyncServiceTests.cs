using Microsoft.Extensions.Logging.Abstractions;
using AoCEditor.Core.Sync.Application;
using Microsoft.Extensions.Time.Testing;
using Shared.Contracts;
using Shouldly;
using Xunit;

namespace AoCEditor.Core.Tests.Sync;

public class SyncServiceTests
{
    private readonly FakeGateway _gateway = new();
    private readonly MemoryStore _store = new();
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 10, 28, 0, 0, 0, TimeSpan.Zero));
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private SyncService Create() => new(_gateway, _store, _time, NullLogger<SyncService>.Instance);

    private void Page(string path, string html) => _gateway.Pages[path] = new GatewayResult.Ok(html);

    [Fact]
    public async Task EventsAreFetchedOnceThenEachYearInOrderAllAsStatisticsWithRefresh()
    {
        Page("/events", Html.Events("/", "/2024"));
        Page("/2025", Html.Calendar(2025, (1, 2)));
        Page("/2024", Html.Calendar(2024, (1, 1)));

        var summary = await Create().SyncAllAsync([], new Progress<SyncProgress>(), Ct);

        _gateway.Requests.ShouldBe([("/events", RequestKind.Statistics, FetchPolicy.Refresh), ("/2025", RequestKind.Statistics, FetchPolicy.Refresh), ("/2024", RequestKind.Statistics, FetchPolicy.Refresh)]);
        summary.Outcome.ShouldBe(SyncOutcome.Completed);
        summary.YearsSynced.ShouldBe([2025, 2024]);
        _store.Years[2024].ShouldBe([new SyncedDay(1, 1)]);
    }

    [Fact]
    public async Task OnlyDaysWhoseStarsChangedAreCounted()
    {
        _store.Years[2024] = [new(1, 2), new(2, 0), new(3, 0)];
        Page("/events", Html.Events("/2024"));
        Page("/2024", Html.Calendar(2024, (1, 2), (2, 1), (3, 0), (4, 0)));

        var summary = await Create().SyncAllAsync([2024], new Progress<SyncProgress>(), Ct);

        summary.DaysUpdated.ShouldBe(2);
    }

    [Fact]
    public async Task AYearWithUnknownMarkupIsReportedAndOtherYearsStillSync()
    {
        Page("/events", Html.Events("/2024", "/2023"));
        Page("/2024", "<html>surprise</html>");
        Page("/2023", Html.Calendar(2023, (1, 2)));

        var summary = await Create().SyncAllAsync([], new Progress<SyncProgress>(), Ct);

        summary.Outcome.ShouldBe(SyncOutcome.Completed);
        summary.YearsSynced.ShouldBe([2023]);
        summary.Errors.ShouldBe([new YearError(2024, SyncError.ParseError)]);
        _store.Years.ContainsKey(2024).ShouldBeFalse();
    }

    [Fact]
    public async Task AFailedYearKeepsItsPreviouslySyncedStars()
    {
        _store.Years[2024] = [new(1, 2)];
        Page("/events", Html.Events("/2024"));
        _gateway.Pages["/2024"] = new GatewayResult.NetworkError();

        var summary = await Create().SyncAllAsync([2024], new Progress<SyncProgress>(), Ct);

        summary.Errors.ShouldBe([new YearError(2024, SyncError.NetworkError)]);
        _store.Years[2024].ShouldBe([new SyncedDay(1, 2)]);
    }

    [Fact]
    public async Task NotSignedInStopsEverythingAndIsReportedDistinctly()
    {
        Page("/events", Html.Events("/2024", "/2023"));
        _gateway.Pages["/2024"] = new GatewayResult.NotSignedIn();

        var summary = await Create().SyncAllAsync([], new Progress<SyncProgress>(), Ct);

        summary.Outcome.ShouldBe(SyncOutcome.NotSignedIn);
        summary.Errors.ShouldBeEmpty();
        _gateway.Requests.Select(r => r.Path).ShouldBe(["/events", "/2024"]);
    }

    [Fact]
    public async Task NotSignedInOnTheEventListMakesNoFurtherRequests()
    {
        _gateway.Pages["/events"] = new GatewayResult.NotSignedIn();

        var summary = await Create().SyncAllAsync([], new Progress<SyncProgress>(), Ct);

        summary.Outcome.ShouldBe(SyncOutcome.NotSignedIn);
        _gateway.Requests.Count.ShouldBe(1);
    }

    [Fact]
    public async Task RateLimitingStopsTheSyncButKeepsEarlierYears()
    {
        Page("/events", Html.Events("/2024", "/2023", "/2022"));
        Page("/2024", Html.Calendar(2024, (1, 1)));
        _gateway.Pages["/2023"] = new GatewayResult.RateLimited();

        var summary = await Create().SyncAllAsync([], new Progress<SyncProgress>(), Ct);

        summary.Outcome.ShouldBe(SyncOutcome.RateLimited);
        summary.YearsSynced.ShouldBe([2024]);
        _gateway.Requests.Count.ShouldBe(3);
    }

    [Theory]
    [InlineData("garbage")]
    public async Task AnUnreadableEventListEndsTheSyncWithoutYearRequests(string html)
    {
        Page("/events", html);

        var summary = await Create().SyncAllAsync([], new Progress<SyncProgress>(), Ct);

        summary.Outcome.ShouldBe(SyncOutcome.EventsUnavailable);
        _gateway.Requests.Count.ShouldBe(1);
    }

    [Fact]
    public async Task ProgressNamesTheYearTheYearsLeftAndTheSecondsToWait()
    {
        Page("/events", Html.Events("/2024", "/2023"));
        Page("/2024", Html.Calendar(2024, (1, 1)));
        Page("/2023", Html.Calendar(2023, (1, 1)));
        _gateway.NextAllowed = _time.GetUtcNow().AddSeconds(90.2);
        var seen = new List<SyncProgress>();

        await Create().SyncAllAsync([], new ImmediateProgress(seen.Add), Ct);

        seen.ShouldBe([new SyncProgress(2024, 1, 91), new SyncProgress(2023, 0, 91)]);
    }

    [Fact]
    public async Task SyncAllSkipsYearsAlreadySynced()
    {
        _store.Years[2023] = [new(1, 2)];
        Page("/events", Html.Events("/2024", "/2023"));
        Page("/2024", Html.Calendar(2024, (1, 1)));

        var summary = await Create().SyncAllAsync([], new Progress<SyncProgress>(), Ct);

        _gateway.Requests.Select(r => r.Path).ShouldBe(["/events", "/2024"]);
        summary.YearsSkipped.ShouldBe([2023]);
        summary.YearsSynced.ShouldBe([2024]);
    }

    [Fact]
    public async Task SyncAllRefreshesOnlyTheYearsNamedByTheCaller()
    {
        _store.Years[2023] = [new(1, 0)];
        _store.Years[2022] = [new(1, 0)];
        Page("/events", Html.Events("/2024", "/2023", "/2022"));
        Page("/2024", Html.Calendar(2024, (1, 1)));
        Page("/2023", Html.Calendar(2023, (1, 2)));

        await Create().SyncAllAsync([2023], new Progress<SyncProgress>(), Ct);

        _gateway.Requests.Select(r => r.Path).ShouldBe(["/events", "/2024", "/2023"]);
        _store.Years[2023].ShouldBe([new SyncedDay(1, 2)]);
    }

    [Fact]
    public async Task SyncYearDoesNotAskForTheEventListAndSkipsASyncedYear()
    {
        _store.Years[2023] = [new(1, 2)];
        Page("/2024", Html.Calendar(2024, (1, 1)));

        await Create().SyncYearAsync(2024, refresh: false, new Progress<SyncProgress>(), Ct);
        var again = await Create().SyncYearAsync(2024, refresh: false, new Progress<SyncProgress>(), Ct);
        var other = await Create().SyncYearAsync(2023, refresh: false, new Progress<SyncProgress>(), Ct);

        _gateway.Requests.Select(r => r.Path).ShouldBe(["/2024"]);
        again.YearsSkipped.ShouldBe([2024]);
        other.YearsSkipped.ShouldBe([2023]);
    }

    [Fact]
    public async Task SyncYearWithRefreshFetchesASyncedYearAgain()
    {
        _store.Years[2023] = [new(1, 0)];
        Page("/2023", Html.Calendar(2023, (1, 2)));

        await Create().SyncYearAsync(2023, refresh: true, new Progress<SyncProgress>(), Ct);

        _store.Years[2023].ShouldBe([new SyncedDay(1, 2)]);
    }

    [Fact]
    public async Task CancellingWhileTheGatewayIsWaitingStopsTheSync()
    {
        Page("/events", Html.Events("/2024"));
        _gateway.Hold = true;
        using var cts = new CancellationTokenSource();

        var sync = Create().SyncAllAsync([], new Progress<SyncProgress>(), cts.Token);
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => sync);
    }

    private sealed class ImmediateProgress(Action<SyncProgress> report) : IProgress<SyncProgress>
    {
        public void Report(SyncProgress value) => report(value);
    }
}
