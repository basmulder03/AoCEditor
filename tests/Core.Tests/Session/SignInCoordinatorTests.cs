using AoCEditor.Core.Session.Application;
using Microsoft.Extensions.Time.Testing;
using Shared.Contracts;
using Shouldly;
using Xunit;

namespace AoCEditor.Core.Tests.Session;

public sealed class SignInCoordinatorTests
{
    private const string Token = "0123abcdef";
    private readonly FakeSecretStore _store = new();
    private readonly FakeGateway _gateway = new();
    private readonly SessionTokenProvider _tokens;
    private bool _profileCleared;
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public SignInCoordinatorTests() => _tokens = new SessionTokenProvider(_store);

    private SignInCoordinator Create(FakeSecretStore? store = null) =>
        new(store is null ? _tokens : new SessionTokenProvider(store), _gateway, () => _profileCleared = true);

    [Fact]
    public async Task BrowserTokenThatTheSiteAcceptsIsSaved()
    {
        var result = await Create().SignInWithBrowserAsync(new FakeBrowser(new BrowserOutcome.Token(Token)), Ct);

        result.ShouldBeOfType<SignInResult.SignedIn>();
        (await _tokens.GetAsync(Ct)).ShouldBe(Token);
        _gateway.Checked.ShouldBe([Token]);
    }

    [Fact]
    public async Task RejectedCandidateKeepsTheBrowserOpenUntilAValidOneArrives()
    {
        _gateway.Answers.Enqueue(new GatewayResult.NotSignedIn());
        var browser = new FakeBrowser(new BrowserOutcome.Cancelled()) { Candidates = ["stale", Token] };

        var result = await Create().SignInWithBrowserAsync(browser, Ct);

        result.ShouldBeOfType<SignInResult.SignedIn>();
        browser.Verdicts.ShouldBe([CandidateVerdict.Rejected, CandidateVerdict.Accepted]);
        _gateway.Checked.ShouldBe(["stale", Token]);
        (await _tokens.GetAsync(Ct)).ShouldBe(Token);
    }

    [Fact]
    public async Task UnreachableSiteDuringCandidateCheckKeepsWaiting()
    {
        _gateway.Answers.Enqueue(new GatewayResult.NetworkError());
        var browser = new FakeBrowser(new BrowserOutcome.Cancelled()) { Candidates = [Token, Token] };

        var result = await Create().SignInWithBrowserAsync(browser, Ct);

        result.ShouldBeOfType<SignInResult.SignedIn>();
        browser.Verdicts.ShouldBe([CandidateVerdict.Unreachable, CandidateVerdict.Accepted]);
    }

    [Fact]
    public async Task AcceptedCandidateIsCheckedOnlyOnce()
    {
        var browser = new FakeBrowser(new BrowserOutcome.Cancelled()) { Candidates = [Token] };

        await Create().SignInWithBrowserAsync(browser, Ct);

        _gateway.Checked.ShouldBe([Token]);
    }

    [Fact]
    public async Task BrowserThatNeverFinishesTimesOut()
    {
        var time = new FakeTimeProvider();
        var browser = new FakeBrowser(new BrowserOutcome.Cancelled()) { WaitsForCancel = true };
        var coordinator = new SignInCoordinator(_tokens, _gateway, null, time, TimeSpan.FromMinutes(10));

        var pending = coordinator.SignInWithBrowserAsync(browser, Ct);
        while (!browser.Opened)
        {
            await Task.Yield();
        }

        time.Advance(TimeSpan.FromMinutes(9));
        pending.IsCompleted.ShouldBeFalse();
        time.Advance(TimeSpan.FromMinutes(2));

        (await pending).ShouldBeOfType<SignInResult.TimedOut>();
        (await _tokens.GetAsync(Ct)).ShouldBeNull();
    }

    [Fact]
    public async Task CancellingTheCallIsCancelledNotTimedOut()
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(Ct);
        var browser = new FakeBrowser(new BrowserOutcome.Cancelled()) { WaitsForCancel = true };

        var pending = Create().SignInWithBrowserAsync(browser, cts.Token);
        await cts.CancelAsync();

        (await pending).ShouldBeOfType<SignInResult.Cancelled>();
    }

    [Fact]
    public async Task TokenThatTheSiteRejectsIsNeverSaved()
    {
        _gateway.Answer = new GatewayResult.NotSignedIn();

        var result = await Create().SignInWithPastedTokenAsync(Token, Ct);

        result.ShouldBeOfType<SignInResult.InvalidToken>();
        (await _tokens.GetAsync(Ct)).ShouldBeNull();
    }

    [Theory]
    [InlineData("  0123abcdef\n")]
    [InlineData("\"0123abcdef\"")]
    [InlineData("'0123abcdef'")]
    [InlineData(" session=0123abcdef ")]
    public async Task PastedTokenIsCleanedBeforeTheCheck(string pasted)
    {
        await Create().SignInWithPastedTokenAsync(pasted, Ct);

        _gateway.Checked.ShouldBe([Token]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc def")]
    [InlineData("abc;def")]
    [InlineData("abc\r\nX-Evil: 1")]
    public async Task MalformedTokenIsRejectedWithoutARequest(string pasted)
    {
        var result = await Create().SignInWithPastedTokenAsync(pasted, Ct);

        result.ShouldBeOfType<SignInResult.InvalidToken>();
        _gateway.Checked.ShouldBeEmpty();
    }

    [Fact]
    public async Task ClosedBrowserIsCancelledAndNothingIsChecked()
    {
        var result = await Create().SignInWithBrowserAsync(new FakeBrowser(new BrowserOutcome.Cancelled()), Ct);

        result.ShouldBeOfType<SignInResult.Cancelled>();
        _gateway.Checked.ShouldBeEmpty();
    }

    [Fact]
    public async Task UnsupportedBrowserReportsWhy()
    {
        var result = await Create().SignInWithBrowserAsync(new FakeBrowser(new BrowserOutcome.Unsupported("no webkit")), Ct);

        result.ShouldBeOfType<SignInResult.BrowserUnsupported>().Reason.ShouldBe("no webkit");
    }

    [Fact]
    public async Task UnavailableSecretStoreIsReportedBeforeBrowsingOrChecking()
    {
        var browser = new FakeBrowser(new BrowserOutcome.Token(Token));

        var result = await Create(new FakeSecretStore { Unavailable = true }).SignInWithBrowserAsync(browser, Ct);

        result.ShouldBeOfType<SignInResult.StoreUnavailable>().Reason.ShouldBe("no service");
        browser.Opened.ShouldBeFalse();
        _gateway.Checked.ShouldBeEmpty();
    }

    [Fact]
    public async Task NetworkTroubleIsReportedAndNothingIsSaved()
    {
        _gateway.Answer = new GatewayResult.NetworkError();

        var result = await Create().SignInWithPastedTokenAsync(Token, Ct);

        result.ShouldBeOfType<SignInResult.NetworkError>();
        (await _tokens.GetAsync(Ct)).ShouldBeNull();
    }

    [Fact]
    public async Task ResultTextNeverContainsTheToken()
    {
        var outcome = new BrowserOutcome.Token(Token);

        outcome.ToString().ShouldNotContain(Token);
        (await Create().SignInWithPastedTokenAsync(Token, Ct)).ToString().ShouldNotContain(Token);
    }

    [Fact]
    public async Task SignOutDeletesTheTokenAndClearsTheBrowserProfile()
    {
        await Create().SignInWithPastedTokenAsync(Token, Ct);

        var result = await Create().SignOutAsync(Ct);

        result.ShouldBeOfType<SecretResult.Ok>();
        (await _tokens.GetAsync(Ct)).ShouldBeNull();
        _profileCleared.ShouldBeTrue();
    }

    [Fact]
    public async Task StatusFollowsTheStore()
    {
        var coordinator = Create();
        (await coordinator.GetStatusAsync(Ct)).State.ShouldBe(SessionState.SignedOut);

        await coordinator.SignInWithPastedTokenAsync(Token, Ct);
        (await coordinator.GetStatusAsync(Ct)).State.ShouldBe(SessionState.SignedIn);

        var down = await Create(new FakeSecretStore { Unavailable = true }).GetStatusAsync(Ct);
        down.State.ShouldBe(SessionState.StoreUnavailable);
        down.Reason.ShouldBe("no service");
    }

    [Fact]
    public void EventsReachSubscribers()
    {
        var events = new SessionEvents();
        var raised = 0;
        events.SignInRequired += (_, _) => raised++;

        events.NotifySignInRequired();

        raised.ShouldBe(1);
    }
}
