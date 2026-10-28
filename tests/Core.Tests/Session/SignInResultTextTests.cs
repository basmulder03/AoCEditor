using AoCEditor.Core.Session.Application;
using Shouldly;
using Xunit;

namespace AoCEditor.Core.Tests.Session;

public sealed class SignInResultTextTests
{
    [Fact]
    public void UnsupportedBrowserShowsTheRealReason()
    {
        SignInResultText.For(new SignInResult.BrowserUnsupported("libwebkit missing")).ShouldContain("libwebkit missing");
    }

    [Fact]
    public void NetworkErrorShowsItsReasonWhenKnown()
    {
        SignInResultText.For(new SignInResult.NetworkError("HTTP 500")).ShouldContain("HTTP 500");
        SignInResultText.For(new SignInResult.NetworkError()).ShouldNotContain("(");
    }

    [Fact]
    public void StoreProblemShowsItsReason()
    {
        SignInResultText.For(new SignInResult.StoreUnavailable("no keyring")).ShouldContain("no keyring");
    }

    [Fact]
    public void EveryResultHasAMessage()
    {
        SignInResult[] all = [new SignInResult.SignedIn(), new SignInResult.Cancelled(), new SignInResult.TimedOut(), new SignInResult.InvalidToken()];

        all.ShouldAllBe(r => SignInResultText.For(r).Length > 0);
    }
}
