using Microsoft.Extensions.Time.Testing;
using Puzzle.Application;
using Puzzle.UI;
using Shared.Contracts;
using Shouldly;
using Xunit;

namespace Puzzle.Tests;

public class DayViewModelTests
{
    private const string Part1 = "<article class=\"day-desc\"><h2>--- Day 3: Trees ---</h2><p>Go.</p></article>";
    private const string Part2 = "<article class=\"day-desc\"><h2>--- Part Two ---</h2><p>Again.</p></article>";

    private readonly FakeGateway _gateway = new();
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2020, 12, 3, 12, 0, 0, TimeSpan.Zero));
    private readonly DayViewModel _model;

    public DayViewModelTests() =>
        _model = new DayViewModel(new PuzzleService(_gateway, new FakeAnswerStore(), _time), 2020, 3, _time);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task LoadingShowsTheTitleAndOnlyTheUnlockedParts()
    {
        _gateway.Reply = new GatewayResult.Ok(Part1);

        await _model.LoadAsync(false, Ct);

        _model.Heading.ShouldBe("Day 3: Trees");
        _model.Parts.Count.ShouldBe(1);
        _model.OpenPart.ShouldBe(1);
        _model.IsComplete.ShouldBeFalse();
    }

    [Fact]
    public async Task ASolvedDayIsCompleteAndCannotBeSubmitted()
    {
        _gateway.Reply = new GatewayResult.Ok(Part1 + "<p>Your puzzle answer was <code>1</code>.</p>" + Part2 + "<p>Your puzzle answer was <code>2</code>.</p>");
        await _model.LoadAsync(false, Ct);
        _model.Answer = "3";

        _model.IsComplete.ShouldBeTrue();
        _model.CanSubmit.ShouldBeFalse();
    }

    [Fact]
    public async Task ALoadFailureIsExplainedInPlainWords()
    {
        _gateway.Reply = new GatewayResult.NotSignedIn();

        await _model.LoadAsync(false, Ct);

        _model.Parts.ShouldBeEmpty();
        _model.Status.ShouldContain("Sign in");
    }

    [Fact]
    public async Task SubmittingNeedsAnAnswerAndNoRequestInFlight()
    {
        _gateway.Reply = new GatewayResult.Ok(Part1);
        await _model.LoadAsync(false, Ct);

        _model.CanSubmit.ShouldBeFalse();
        _model.Answer = "  ";
        _model.CanSubmit.ShouldBeFalse();
        _model.Answer = "42";
        _model.CanSubmit.ShouldBeTrue();
    }

    [Fact]
    public async Task ACorrectPartOneClearsTheAnswerAndShowsPartTwo()
    {
        _gateway.Reply = new GatewayResult.Ok(Part1);
        await _model.LoadAsync(false, Ct);
        _gateway.PostReply = new GatewayResult.Ok("<article><p>That's the right answer!</p></article>");
        _gateway.GetReply = new GatewayResult.Ok(Part1 + Part2);
        _model.Answer = "42";

        await _model.SubmitAsync(Ct);

        _model.Result.ShouldContain("right");
        _model.Answer.ShouldBeEmpty();
        _model.Parts.Count.ShouldBe(2);
        _model.OpenPart.ShouldBe(2);
    }

    [Fact]
    public async Task ARateLimitShowsACountdownThatTicksDownAndReenablesSubmit()
    {
        _gateway.Reply = new GatewayResult.Ok(Part1);
        await _model.LoadAsync(false, Ct);
        _gateway.PostReply = new GatewayResult.Ok("<article><p>You gave an answer too recently. You have 30s left to wait.</p></article>");
        _model.Answer = "42";
        await _model.SubmitAsync(Ct);

        _model.CooldownSeconds.ShouldBe(30);
        _model.CanSubmit.ShouldBeFalse();
        _model.Answer.ShouldBe("42");

        _time.Advance(TimeSpan.FromSeconds(12));
        _model.Tick();
        _model.CooldownSeconds.ShouldBe(18);

        _time.Advance(TimeSpan.FromSeconds(20));
        _model.Tick();
        _model.CooldownSeconds.ShouldBe(0);
        _model.CanSubmit.ShouldBeTrue();
    }

    [Fact]
    public async Task ARepeatedWrongAnswerIsExplainedWithoutARequest()
    {
        _gateway.Reply = new GatewayResult.Ok(Part1);
        await _model.LoadAsync(false, Ct);
        _gateway.PostReply = new GatewayResult.Ok("<article><p>That's not the right answer; your answer is too low.</p></article>");
        _model.Answer = "42";
        await _model.SubmitAsync(Ct);
        _model.Result.ShouldContain("too low");

        await _model.SubmitAsync(Ct);

        _model.Result.ShouldContain("already tried");
        _gateway.Posts.Count.ShouldBe(1);
    }

    [Fact]
    public async Task TheInputLoadsOnDemand()
    {
        _gateway.Reply = new GatewayResult.Ok("1\n2\n");

        await _model.LoadInputAsync(false, Ct);

        _model.InputText.ShouldBe("1\n2\n");
    }

    [Fact]
    public async Task ACorrectPartOneWithoutTheRefreshedPuzzleBlocksFurtherSubmitsUntilReload()
    {
        _gateway.Reply = new GatewayResult.Ok(Part1);
        await _model.LoadAsync(false, Ct);
        _gateway.PostReply = new GatewayResult.Ok("<article><p>That's the right answer!</p></article>");
        _gateway.GetReply = new GatewayResult.NetworkError("down");
        _model.Answer = "42";
        await _model.SubmitAsync(Ct);

        _model.Answer = "43";
        _model.CanSubmit.ShouldBeFalse();
        _model.Status.ShouldContain("Reload");

        _gateway.GetReply = new GatewayResult.Ok(Part1 + Part2);
        await _model.LoadAsync(true, Ct);

        _model.CanSubmit.ShouldBeTrue();
    }
}
