using Puzzle.Application;
using Puzzle.Domain;
using Shared.Contracts;
using Microsoft.Extensions.Time.Testing;
using Shouldly;
using Xunit;

namespace Puzzle.Tests;

public class PuzzleServiceTests
{
    private const string PuzzleHtml = "<article class=\"day-desc\"><h2>--- Day 3: Trees ---</h2><p>Go.</p></article>";

    private readonly FakeGateway _gateway = new();
    private readonly FakeAnswerStore _store = new();
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2020, 12, 3, 12, 0, 0, TimeSpan.Zero));
    private readonly PuzzleService _service;

    public PuzzleServiceTests() => _service = new PuzzleService(_gateway, _store, _time);

    [Fact]
    public async Task ThePuzzleComesFromTheDayPageAsAnInteractiveCacheFirstRequest()
    {
        _gateway.Reply = new GatewayResult.Ok(PuzzleHtml);

        var result = await _service.LoadPuzzleAsync(2020, 3, refresh: false, CancellationToken.None);

        result.Status.ShouldBe(FetchStatus.Ok);
        result.Value!.Title.ShouldBe("Trees");
        _gateway.Gets.ShouldBe([("/2020/day/3", RequestKind.Interactive, FetchPolicy.CacheFirst)]);
    }

    [Fact]
    public async Task ARefreshAsksTheServer()
    {
        _gateway.Reply = new GatewayResult.Ok(PuzzleHtml);

        await _service.LoadPuzzleAsync(2020, 3, refresh: true, CancellationToken.None);

        _gateway.Gets.Single().Policy.ShouldBe(FetchPolicy.Refresh);
    }

    [Fact]
    public async Task APageWithoutAPuzzleIsUnreadable()
    {
        _gateway.Reply = new GatewayResult.Ok("<html>[Log In]</html>");

        (await _service.LoadPuzzleAsync(2020, 3, false, CancellationToken.None)).Status.ShouldBe(FetchStatus.Unreadable);
    }

    [Theory]
    [MemberData(nameof(Failures))]
    public async Task GatewayFailuresMapToAStatus(GatewayResult reply, FetchStatus expected)
    {
        _gateway.Reply = reply;

        (await _service.LoadPuzzleAsync(2020, 3, false, CancellationToken.None)).Status.ShouldBe(expected);
        (await _service.LoadInputAsync(2020, 3, false, CancellationToken.None)).Status.ShouldBe(expected);
    }

    public static TheoryData<GatewayResult, FetchStatus> Failures => new()
    {
        { new GatewayResult.NotSignedIn(), FetchStatus.NotSignedIn },
        { new GatewayResult.RateLimited(), FetchStatus.RateLimited },
        { new GatewayResult.NotFound(), FetchStatus.NotFound },
        { new GatewayResult.NetworkError("down"), FetchStatus.NetworkError },
    };

    [Fact]
    public async Task TheInputIsTheRawTextOfTheInputPath()
    {
        _gateway.Reply = new GatewayResult.Ok("1\n2\n");

        var result = await _service.LoadInputAsync(2020, 3, false, CancellationToken.None);

        result.Value.ShouldBe("1\n2\n");
        _gateway.Gets.Single().Path.ShouldBe("/2020/day/3/input");
    }

    [Fact]
    public async Task ASubmissionPostsTheLevelAndTheTrimmedAnswerOnce()
    {
        _gateway.Reply = new GatewayResult.Ok("<article><p>That's not the right answer.</p></article>");

        var result = await _service.SubmitAsync(2020, 3, part: 2, " 42 ", CancellationToken.None);

        result.Value!.Verdict.Outcome.ShouldBe(SubmitOutcome.Wrong);
        _gateway.Posts.Single().Path.ShouldBe("/2020/day/3/answer");
        _gateway.Posts.Single().Form.ShouldBe(new Dictionary<string, string> { ["level"] = "2", ["answer"] = "42" });
    }

    [Fact]
    public async Task ACorrectPartOneRefreshesThePuzzleOnceToPickUpPartTwo()
    {
        _gateway.Reply = new GatewayResult.Ok("<article><p>That's the right answer!</p></article>");
        _gateway.GetReply = new GatewayResult.Ok(PuzzleHtml + PuzzleHtml);

        var result = await _service.SubmitAsync(2020, 3, part: 1, "7", CancellationToken.None);

        result.Value!.Unlocked!.Parts.Count.ShouldBe(2);
        _gateway.Gets.ShouldBe([("/2020/day/3", RequestKind.Interactive, FetchPolicy.Refresh)]);
    }

    [Theory]
    [InlineData(1, "That's not the right answer.")]
    [InlineData(2, "That's the right answer!")]
    public async Task OtherSubmissionsDoNotFetchAgain(int part, string message)
    {
        _gateway.Reply = new GatewayResult.Ok($"<article><p>{message}</p></article>");

        (await _service.SubmitAsync(2020, 3, part, "7", CancellationToken.None)).Value!.Unlocked.ShouldBeNull();

        _gateway.Gets.ShouldBeEmpty();
    }

    [Fact]
    public async Task EveryFinalAnswerIsRecordedAndACorrectOneBecomesTheKnownAnswer()
    {
        _gateway.Reply = new GatewayResult.Ok("<article><p>That's not the right answer.</p></article>");
        await _service.SubmitAsync(2020, 3, 1, "5", CancellationToken.None);
        _gateway.Reply = new GatewayResult.Ok("<article><p>That's the right answer!</p></article>");
        _gateway.GetReply = new GatewayResult.Ok(PuzzleHtml);
        await _service.SubmitAsync(2020, 3, 1, "6", CancellationToken.None);

        _store.Attempts.Select(a => (a.Answer, a.Outcome)).ShouldBe([("5", "Wrong"), ("6", "Correct")]);
        _store.Attempts[0].At.ShouldBe(_time.GetUtcNow());
        _store.Known[(2020, 3, 1)].ShouldBe("6");
    }

    [Fact]
    public async Task UnrecognisedResponsesAreNotRecorded()
    {
        _gateway.Reply = new GatewayResult.Ok("<article><p>Something new</p></article>");

        (await _service.SubmitAsync(2020, 3, 1, "5", CancellationToken.None)).Value!.Verdict.Outcome.ShouldBe(SubmitOutcome.Unknown);

        _store.Attempts.ShouldBeEmpty();
    }

    [Fact]
    public async Task ARateLimitStartsACooldownThatBlocksTheNextSubmitWithoutARequest()
    {
        _gateway.Reply = new GatewayResult.Ok("<article><p>You gave an answer too recently. You have 1m 30s left to wait.</p></article>");
        await _service.SubmitAsync(2020, 3, 1, "5", CancellationToken.None);

        (await _service.CooldownUntilAsync(2020, 3, 1, CancellationToken.None)).ShouldBe(_time.GetUtcNow().AddSeconds(90));
        var blocked = await _service.SubmitAsync(2020, 3, 1, "6", CancellationToken.None);

        blocked.Status.ShouldBe(FetchStatus.CoolingDown);
        _gateway.Posts.Count.ShouldBe(1);

        _time.Advance(TimeSpan.FromSeconds(91));
        (await _service.CooldownUntilAsync(2020, 3, 1, CancellationToken.None)).ShouldBeNull();
        (await _service.SubmitAsync(2020, 3, 1, "6", CancellationToken.None)).Status.ShouldBe(FetchStatus.Ok);
    }

    [Fact]
    public async Task AWrongAnswerIsNotSentTwice()
    {
        _gateway.Reply = new GatewayResult.Ok("<article><p>That's not the right answer; your answer is too high.</p></article>");
        await _service.SubmitAsync(2020, 3, 1, "5", CancellationToken.None);

        var again = await _service.SubmitAsync(2020, 3, 1, " 5 ", CancellationToken.None);

        again.Status.ShouldBe(FetchStatus.AlreadyTried);
        _gateway.Posts.Count.ShouldBe(1);
    }

    [Theory]
    [InlineData(0, "1")]
    [InlineData(3, "1")]
    [InlineData(1, "  ")]
    public async Task InvalidSubmissionsNeverReachTheNetwork(int part, string answer)
    {
        await Should.ThrowAsync<ArgumentException>(() => _service.SubmitAsync(2020, 3, part, answer, CancellationToken.None));

        _gateway.Posts.ShouldBeEmpty();
    }
}
