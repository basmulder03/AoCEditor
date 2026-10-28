using Microsoft.Extensions.Time.Testing;
using Shared.Contracts;
using Shouldly;
using Sync.Application;
using Xunit;

namespace Sync.Tests;

public class SyncServiceTests
{
    private readonly FakeGateway _gateway = new();
    private readonly MemoryStore _store = new();
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 10, 28, 0, 0, 0, TimeSpan.Zero));
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private SyncService Create() => new(_gateway, _store, _time);

    private void Page(string path, string html) => _gateway.Pages[path] = new GatewayResult.Ok(html);

    [Fact]
    public async Task EventsAreFetchedOnceThenEachYearInOrderAllWithRefresh()
    {
        Page("/events", Html.Events("/", "/2024"));
        Page("/2025", Html.Calendar(2025, (1, 2)));
        Page("/2024", Html.Calendar(2024, (1, 1)));

        var summary = await Create().SyncAsync(new Progress<SyncProgress>(), Ct);

        _gateway.Requests.ShouldBe([("/events", FetchPolicy.Refresh), ("/2025", FetchPolicy.Refresh), ("/2024", FetchPolicy.Refresh)]);
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

        var summary = await Create().SyncAsync(new Progress<SyncProgress>(), Ct);

        summary.DaysUpdated.ShouldBe(2);
    }

    [Fact]
    public async Task AYearWithUnknownMarkupIsReportedAndOtherYearsStillSync()
    {
        Page("/events", Html.Events("/2024", "/2023"));
        Page("/2024", "<html>surprise</html>");
        Page("/2023", Html.Calendar(2023, (1, 2)));

        var summary = await Create().SyncAsync(new Progress<SyncProgress>(), Ct);

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

        var summary = await Create().SyncAsync(new Progress<SyncProgress>(), Ct);

        summary.Errors.ShouldBe([new YearError(2024, SyncError.NetworkError)]);
        _store.Years[2024].ShouldBe([new SyncedDay(1, 2)]);
    }

    [Fact]
    public async Task NotSignedInStopsEverythingAndIsReportedDistinctly()
    {
        Page("/events", Html.Events("/2024", "/2023"));
        _gateway.Pages["/2024"] = new GatewayResult.NotSignedIn();

        var summary = await Create().SyncAsync(new Progress<SyncProgress>(), Ct);

        summary.Outcome.ShouldBe(SyncOutcome.NotSignedIn);
        summary.Errors.ShouldBeEmpty();
        _gateway.Requests.Select(r => r.Path).ShouldBe(["/events", "/2024"]);
    }

    [Fact]
    public async Task NotSignedInOnTheEventListMakesNoFurtherRequests()
    {
        _gateway.Pages["/events"] = new GatewayResult.NotSignedIn();

        var summary = await Create().SyncAsync(new Progress<SyncProgress>(), Ct);

        summary.Outcome.ShouldBe(SyncOutcome.NotSignedIn);
        _gateway.Requests.Count.ShouldBe(1);
    }

    [Fact]
    public async Task RateLimitingStopsTheSyncButKeepsEarlierYears()
    {
        Page("/events", Html.Events("/2024", "/2023", "/2022"));
        Page("/2024", Html.Calendar(2024, (1, 1)));
        _gateway.Pages["/2023"] = new GatewayResult.RateLimited();

        var summary = await Create().SyncAsync(new Progress<SyncProgress>(), Ct);

        summary.Outcome.ShouldBe(SyncOutcome.RateLimited);
        summary.YearsSynced.ShouldBe([2024]);
        _gateway.Requests.Count.ShouldBe(3);
    }

    [Theory]
    [InlineData("garbage")]
    public async Task AnUnreadableEventListEndsTheSyncWithoutYearRequests(string html)
    {
        Page("/events", html);

        var summary = await Create().SyncAsync(new Progress<SyncProgress>(), Ct);

        summary.Outcome.ShouldBe(SyncOutcome.EventsUnavailable);
        _gateway.Requests.Count.ShouldBe(1);
    }

    [Fact]
    public async Task ProgressIsReportedAfterEachYear()
    {
        Page("/events", Html.Events("/2024", "/2023"));
        Page("/2024", Html.Calendar(2024, (1, 1)));
        Page("/2023", Html.Calendar(2023, (1, 1)));
        var seen = new List<SyncProgress>();

        await Create().SyncAsync(new ImmediateProgress(seen.Add), Ct);

        seen.ShouldBe([new SyncProgress(2024, 1, 2), new SyncProgress(2023, 2, 2)]);
    }

    private sealed class ImmediateProgress(Action<SyncProgress> report) : IProgress<SyncProgress>
    {
        public void Report(SyncProgress value) => report(value);
    }
}
