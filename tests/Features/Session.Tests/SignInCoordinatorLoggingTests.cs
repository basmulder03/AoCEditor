using Microsoft.Extensions.Time.Testing;
using Session.Application;
using Shared.Contracts;
using Shouldly;
using Xunit;

namespace Session.Tests;

public sealed class SignInCoordinatorLoggingTests
{
    private const string Token = "0123abcdef-secret";
    private readonly CapturingLogger _log = new();
    private readonly FakeGateway _gateway = new();
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private SignInCoordinator Create() => new(new SessionTokenProvider(new FakeSecretStore()), _gateway, null, null, null, _log);

    [Fact]
    public async Task UnsupportedBrowserIsLoggedWithItsReason()
    {
        await Create().SignInWithBrowserAsync(new FakeBrowser(new BrowserOutcome.Unsupported("no cookie access")), Ct);

        _log.Lines.ShouldContain(l => l.StartsWith("Warning", StringComparison.Ordinal) && l.Contains("no cookie access", StringComparison.Ordinal));
    }

    [Fact]
    public async Task CandidateVerdictsAndOutcomeAreLoggedWithoutTheToken()
    {
        _gateway.Answers.Enqueue(new GatewayResult.NotSignedIn());
        var browser = new FakeBrowser(new BrowserOutcome.Cancelled()) { Candidates = [Token, Token] };

        await Create().SignInWithBrowserAsync(browser, Ct);

        var all = string.Join('\n', _log.Lines);
        all.ShouldContain("Rejected");
        all.ShouldContain("Accepted");
        all.ShouldContain("SignedIn");
        all.ShouldNotContain(Token);
    }

    [Fact]
    public async Task NetworkReasonReachesTheResultAndTheLog()
    {
        _gateway.Answer = new GatewayResult.NetworkError("HTTP 503");

        var result = await Create().SignInWithPastedTokenAsync(Token, Ct);

        result.ShouldBeOfType<SignInResult.NetworkError>().Reason.ShouldBe("HTTP 503");
        _log.Lines.ShouldContain(l => l.Contains("HTTP 503", StringComparison.Ordinal));
        string.Join('\n', _log.Lines).ShouldNotContain(Token);
    }
}
