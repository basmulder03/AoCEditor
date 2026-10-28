using Puzzle.Application;
using Puzzle.Domain;
using Shared.Contracts;
using Shouldly;
using Xunit;

namespace Puzzle.Tests;

public class PuzzleServiceTests
{
    private const string PuzzleHtml = "<article class=\"day-desc\"><h2>--- Day 3: Trees ---</h2><p>Go.</p></article>";

    private readonly FakeGateway _gateway = new();
    private readonly PuzzleService _service;

    public PuzzleServiceTests() => _service = new PuzzleService(_gateway);

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
        _gateway.Reply = new GatewayResult.Ok("<article><p>That's the right answer!</p></article>");

        var result = await _service.SubmitAsync(2020, 3, part: 2, " 42 ", CancellationToken.None);

        result.Value!.Outcome.ShouldBe(SubmitOutcome.Correct);
        _gateway.Posts.Single().Path.ShouldBe("/2020/day/3/answer");
        _gateway.Posts.Single().Form.ShouldBe(new Dictionary<string, string> { ["level"] = "2", ["answer"] = "42" });
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

    private sealed class FakeGateway : IAocGateway
    {
        public GatewayResult Reply { get; set; } = new GatewayResult.NotFound();

        public List<(string Path, RequestKind Kind, FetchPolicy Policy)> Gets { get; } = [];

        public List<(string Path, IReadOnlyDictionary<string, string> Form)> Posts { get; } = [];

        public Task<GatewayResult> GetAsync(string path, RequestKind kind, FetchPolicy policy, CancellationToken cancellationToken)
        {
            Gets.Add((path, kind, policy));
            return Task.FromResult(Reply);
        }

        public Task<GatewayResult> PostFormAsync(string path, IReadOnlyDictionary<string, string> form, CancellationToken cancellationToken)
        {
            Posts.Add((path, form));
            return Task.FromResult(Reply);
        }

        public Task<GatewayResult> CheckTokenAsync(string token, CancellationToken cancellationToken) => Task.FromResult(Reply);

        public DateTimeOffset NextAllowedAt(RequestKind kind) => DateTimeOffset.MinValue;
    }
}
