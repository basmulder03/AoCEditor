using AoCEditor.Core.Puzzle.Application;
using AoCEditor.UI.Puzzle;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Microsoft.Extensions.Time.Testing;
using Shared.Contracts;
using Shouldly;

namespace AoCEditor.UI.Tests;

public class DayViewTests
{
    private const string Part1 = "<article class=\"day-desc\"><h2>--- Day 3: Trees ---</h2><p>Go.</p></article>";
    private const string Part2 = "<article class=\"day-desc\"><h2>--- Part Two ---</h2><p>Again.</p></article>";

    private static async Task<(DayView View, DayViewModel Model)> ShowAsync(string html)
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2020, 12, 3, 12, 0, 0, TimeSpan.Zero));
        var model = new DayViewModel(new PuzzleService(new Gateway(html), new Answers(), time), 2020, 3, time);
        await model.LoadAsync(false, CancellationToken.None);
        var view = new DayView { DataContext = model };
        new Window { Content = view, Width = 800, Height = 600 }.Show();
        return (view, model);
    }

    [AvaloniaFact]
    public async Task OneTabPerUnlockedPartPlusTheInput()
    {
        var (view, _) = await ShowAsync(Part1);

        view.Tabs.Items.OfType<TabItem>().Select(t => t.Header).ShouldBe(["Part 1", "Input"]);
    }

    [AvaloniaFact]
    public async Task PartTwoGetsATabOnceItIsUnlocked()
    {
        var (view, _) = await ShowAsync(Part1 + Part2);

        view.Tabs.Items.OfType<TabItem>().Select(t => t.Header).ShouldBe(["Part 1", "Part 2", "Input"]);
    }

    [AvaloniaFact]
    public async Task TheSubmitButtonIsNamedAndOnlyEnabledWithAnAnswer()
    {
        var (view, model) = await ShowAsync(Part1);
        var button = view.GetVisualDescendants().OfType<Button>().Single(b => AutomationProperties.GetName(b) == "Submit answer");

        button.IsEffectivelyEnabled.ShouldBeFalse();
        model.Answer = "42";
        button.IsEffectivelyEnabled.ShouldBeTrue();
    }

    private sealed class Gateway(string html) : IAocGateway
    {
        public Task<GatewayResult> GetAsync(string path, RequestKind kind, FetchPolicy policy, CancellationToken cancellationToken) =>
            Task.FromResult<GatewayResult>(new GatewayResult.Ok(html));

        public Task<GatewayResult> PostFormAsync(string path, IReadOnlyDictionary<string, string> form, CancellationToken cancellationToken) =>
            Task.FromResult<GatewayResult>(new GatewayResult.NotFound());

        public Task<GatewayResult> CheckTokenAsync(string token, CancellationToken cancellationToken) =>
            Task.FromResult<GatewayResult>(new GatewayResult.NotFound());

        public DateTimeOffset NextAllowedAt(RequestKind kind) => DateTimeOffset.MinValue;
    }

    private sealed class Answers : IAnswerStore
    {
        public Task AddAttemptAsync(int year, int day, int part, AnswerAttempt attempt, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<IReadOnlyList<AnswerAttempt>> AttemptsAsync(int year, int day, int part, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<AnswerAttempt>>([]);

        public Task SetCooldownAsync(int year, int day, int part, DateTimeOffset until, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<DateTimeOffset?> CooldownUntilAsync(int year, int day, int part, CancellationToken cancellationToken) =>
            Task.FromResult<DateTimeOffset?>(null);

        public Task SetKnownAnswerAsync(int year, int day, int part, string answer, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<string?> KnownAnswerAsync(int year, int day, int part, CancellationToken cancellationToken) => Task.FromResult<string?>(null);
    }
}
