using Microsoft.Extensions.Time.Testing;
using Shared.Contracts;
using Shouldly;
using Sync.Application;
using Sync.UI;
using Xunit;

namespace Sync.Tests;

public class CalendarViewModelTests
{
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 10, 28, 12, 0, 0, TimeSpan.Zero));
    private readonly FakeGateway _gateway = new();
    private readonly MemoryStore _store = new();
    private readonly CapturingLogger _log = new();

    private CalendarViewModel Create() => new(new SyncService(_gateway, _store, _time), _store, _time, _log);

    private static List<SyncedDay> Days(int count, params (int Day, int Stars)[] stars) =>
        [.. Enumerable.Range(1, count).Select(d => new SyncedDay(d, stars.FirstOrDefault(s => s.Day == d).Stars))];

    [Fact]
    public async Task YearsAreNewestFirstAndTheLatestIsSelected()
    {
        _store.Years[2016] = Days(25);
        var vm = Create();
        await vm.LoadAsync(CancellationToken.None);

        vm.Years[0].ShouldBe(2025);
        vm.Years[^1].ShouldBe(2015);
        vm.SelectedYear.ShouldBe(2025);
    }

    [Fact]
    public async Task EachDayShowsItsStateFromStarsAndTheClock()
    {
        _time.SetUtcNow(new DateTimeOffset(2026, 12, 5, 12, 0, 0, TimeSpan.Zero));
        _store.Years[2026] = Days(25, (1, 2), (2, 1));
        var vm = Create();
        await vm.LoadAsync(CancellationToken.None);

        vm.Days.Count.ShouldBe(25);
        vm.Days[0].Name.ShouldBe("Day 1, two stars");
        vm.Days[1].Name.ShouldBe("Day 2, one star");
        vm.Days[2].Name.ShouldBe("Day 3, open");
        vm.Days[7].Name.ShouldBe("Day 8, locked, unlocks Dec 8 at 05:00 UTC");
        vm.Days[7].Label.ShouldBe("Locked");
        vm.StarsText.ShouldBe("3 of 50 stars");
    }

    [Fact]
    public async Task SwitchingYearShowsThatYearsDaysWithoutRequests()
    {
        _store.Years[2025] = Days(12, (1, 2));
        _store.Years[2024] = Days(25, (1, 1));
        var vm = Create();
        await vm.LoadAsync(CancellationToken.None);

        vm.Days.Count.ShouldBe(12);
        await vm.SelectYearAsync(2024, CancellationToken.None);

        vm.SelectedYear.ShouldBe(2024);
        vm.Days.Count.ShouldBe(25);
        vm.Days[0].Label.ShouldBe("One star");
        vm.Days[24].Label.ShouldBe("Open");
        _gateway.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task AYearNeverSyncedShowsTheEmptyState()
    {
        var vm = Create();
        await vm.LoadAsync(CancellationToken.None);

        vm.IsEmpty.ShouldBeTrue();
        vm.Days.ShouldBeEmpty();
        vm.LastSyncedText.ShouldBe("Not synced yet");
    }

    [Fact]
    public async Task SyncingTheYearFillsTheGridAndRecordsTheTime()
    {
        _gateway.Pages["/2025"] = new GatewayResult.Ok(Html.Calendar(2025, [.. Enumerable.Range(1, 12).Select(d => (d, d == 1 ? 2 : 0))]));
        var vm = Create();
        await vm.LoadAsync(CancellationToken.None);
        await vm.SyncYearAsync();

        vm.Days.Count.ShouldBe(12);
        vm.Days[0].Label.ShouldBe("Two stars");
        vm.ResultText.ShouldBe("1 year synced, 12 days updated.");
        vm.LastSyncedText.ShouldBe("Last synced 2026-10-28 12:00 UTC");
        vm.IsSyncing.ShouldBeFalse();
        _log.Lines.ShouldContain(l => l.Contains("Sync of year 2025 requested"));
    }

    [Fact]
    public async Task TheCountdownTicksOncePerSecondAndCancelStopsTheSync()
    {
        _gateway.NextAllowed = _time.GetUtcNow().AddSeconds(30);
        _gateway.Hold = true;
        var vm = Create();
        await vm.LoadAsync(CancellationToken.None);

        var run = vm.SyncYearAsync();
        vm.IsSyncing.ShouldBeTrue();
        vm.SecondsUntilNextRequest.ShouldBe(30);
        vm.ProgressText.ShouldContain("2025");

        _time.Advance(TimeSpan.FromSeconds(3));
        vm.SecondsUntilNextRequest.ShouldBe(27);
        vm.ProgressText.ShouldContain("27");

        vm.Cancel();
        await run;
        vm.IsSyncing.ShouldBeFalse();
        vm.ResultText.ShouldContain("cancelled");
        _time.Advance(TimeSpan.FromSeconds(5));
        vm.SecondsUntilNextRequest.ShouldBe(0);
    }

    [Fact]
    public async Task NotSignedInOffersTheAccountSettings()
    {
        _gateway.Pages["/2025"] = new GatewayResult.NotSignedIn();
        var vm = Create();
        var requested = 0;
        vm.AccountRequested += (_, _) => requested++;
        await vm.LoadAsync(CancellationToken.None);
        await vm.SyncYearAsync();

        vm.NeedsSignIn.ShouldBeTrue();
        vm.ResultText.ShouldContain("not signed in");
        vm.RequestAccount();
        requested.ShouldBe(1);
    }

    [Fact]
    public async Task AYearThatCannotBeReadIsListedWithItsError()
    {
        _gateway.Pages["/events"] = new GatewayResult.Ok(Html.Events("/2025", "/2024"));
        _gateway.Pages["/2025"] = new GatewayResult.Ok("<html>maintenance</html>");
        _gateway.Pages["/2024"] = new GatewayResult.Ok(Html.Calendar(2024, (1, 2)));
        var vm = Create();
        await vm.LoadAsync(CancellationToken.None);
        await vm.SyncAllAsync();

        vm.ResultText.ShouldStartWith("1 year synced");
        vm.ErrorLines.ShouldBe(["2025: the calendar page could not be read."]);
        vm.Years.ShouldContain(2024);
    }

    [Fact]
    public async Task OnlyUnlockedDaysCanBeSelected()
    {
        _time.SetUtcNow(new DateTimeOffset(2026, 12, 2, 12, 0, 0, TimeSpan.Zero));
        _store.Years[2026] = Days(25);
        var vm = Create();
        var picked = new List<(int, int)>();
        vm.DaySelected += (_, e) => picked.Add((e.Year, e.Day));
        await vm.LoadAsync(CancellationToken.None);

        vm.Select(vm.Days[4]);
        vm.Select(vm.Days[1]);

        picked.ShouldBe([(2026, 2)]);
    }
}
