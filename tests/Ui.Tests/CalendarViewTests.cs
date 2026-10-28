using AoCEditor.Core.Sync.Application;
using AoCEditor.UI.Sync;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using Microsoft.Extensions.Time.Testing;
using Shared.Contracts;
using Shouldly;

namespace AoCEditor.UI.Tests;

public class CalendarViewTests
{
    private static async Task<(CalendarView View, CalendarViewModel Model, Window Window)> ShowAsync(int year, int days)
    {
        var time = new FakeTimeProvider(new DateTimeOffset(year, 12, 3, 12, 0, 0, TimeSpan.Zero));
        var store = new Store(year, days);
        var model = new CalendarViewModel(new SyncService(new Gateway(), store, time), store, time);
        await model.LoadAsync(CancellationToken.None);
        var view = new CalendarView { DataContext = model };
        var window = new Window { Content = view, Width = 800, Height = 700 };
        window.Show();
        return (view, model, window);
    }

    [AvaloniaFact]
    public async Task EveryDayHasATileWithAnAnnouncedName()
    {
        var (view, _, _) = await ShowAsync(2025, 12);

        var names = view.Tiles.Select(AutomationProperties.GetName).ToList();
        names.Count.ShouldBe(12);
        names[0].ShouldBe("Day 1, two stars");
        names[3].ShouldBe("Day 4, locked, unlocks Dec 4 at 05:00 UTC");
    }

    [AvaloniaFact]
    public async Task ArrowKeysMoveFocusAcrossTheGrid()
    {
        var (view, _, window) = await ShowAsync(2025, 12);
        var tiles = view.Tiles;
        tiles[0].Focus();

        window.KeyPressQwerty(PhysicalKey.ArrowRight, RawInputModifiers.None);
        tiles[1].IsFocused.ShouldBeTrue();
        window.KeyPressQwerty(PhysicalKey.ArrowDown, RawInputModifiers.None);
        tiles[6].IsFocused.ShouldBeTrue();
        window.KeyPressQwerty(PhysicalKey.ArrowLeft, RawInputModifiers.None);
        tiles[5].IsFocused.ShouldBeTrue();
    }

    [AvaloniaFact]
    public async Task ALockedTileIsFocusableButNotSelectable()
    {
        var (view, model, _) = await ShowAsync(2025, 12);
        var picked = new List<int>();
        model.DaySelected += (_, e) => picked.Add(e.Day);
        var tiles = view.Tiles;

        tiles[11].Focusable.ShouldBeTrue();
        tiles[11].RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        tiles[0].RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));

        picked.ShouldBe([1]);
    }

    [AvaloniaFact]
    public async Task TheYearSwitcherAndSyncButtonsAreNamed()
    {
        var (view, _, _) = await ShowAsync(2025, 12);

        var names = view.GetVisualDescendants().OfType<Control>().Select(AutomationProperties.GetName).ToList();
        names.ShouldContain("Year");
        names.ShouldContain("Sync this year");
        names.ShouldContain("Sync all years");
    }

    private sealed class Gateway : IAocGateway
    {
        public Task<GatewayResult> GetAsync(string path, RequestKind kind, FetchPolicy policy, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<GatewayResult> PostFormAsync(string path, IReadOnlyDictionary<string, string> form, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<GatewayResult> CheckTokenAsync(string token, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public DateTimeOffset NextAllowedAt(RequestKind kind) => DateTimeOffset.MinValue;
    }

    private sealed class Store(int year, int days) : ISyncStateStore
    {
        public Task<IReadOnlyList<SyncedDay>> ReadYearAsync(int y, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<SyncedDay>>(y == year ? [.. Enumerable.Range(1, days).Select(d => new SyncedDay(d, d == 1 ? 2 : 0))] : []);

        public Task ReplaceYearAsync(int y, IReadOnlyList<SyncedDay> d, DateTimeOffset at, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<int>> SyncedYearsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<int>>([year]);

        public Task<DateTimeOffset?> LastSyncedAsync(int y, CancellationToken cancellationToken) =>
            Task.FromResult<DateTimeOffset?>(null);
    }
}
