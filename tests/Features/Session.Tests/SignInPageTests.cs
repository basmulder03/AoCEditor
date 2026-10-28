using Session.Application;
using Shouldly;
using Xunit;

namespace Session.Tests;

public sealed class SignInPageTests
{
    private const string Host = "adventofcode.com";

    [Theory]
    [InlineData("https://adventofcode.com/", true, "abc", "abc")]
    [InlineData("https://adventofcode.com/2024", true, "abc", "abc")]
    [InlineData("https://adventofcode.com/auth/login", true, "stale", null)]
    [InlineData("https://adventofcode.com/auth/github/callback?code=1", true, "abc", null)]
    [InlineData("https://adventofcode.com/", false, "abc", null)]
    [InlineData("https://adventofcode.com/", true, null, null)]
    [InlineData("https://adventofcode.com/", true, "", null)]
    [InlineData("https://github.com/login/oauth", true, "abc", null)]
    [InlineData(null, true, "abc", null)]
    public void OnlyAFinishedPageOutsideAuthWithACookieIsACandidate(string? url, bool finished, string? cookie, string? expected)
    {
        var candidate = SignInPage.Candidate(url is null ? null : new Uri(url), finished, cookie, Host);

        candidate.ShouldBe(expected);
    }
}

public sealed class SignInPageLogTests
{
    [Theory]
    [InlineData("https://adventofcode.com/auth/github/callback?code=SECRET&state=x#frag", "adventofcode.com/auth/github/callback")]
    [InlineData("https://user:pw@adventofcode.com/2024?session=SECRET", "adventofcode.com/2024")]
    [InlineData("https://adventofcode.com", "adventofcode.com/")]
    public void LoggedUrlsCarryOnlyHostAndPath(string url, string expected)
    {
        SignInPage.HostAndPath(new Uri(url)).ShouldBe(expected);
    }

    [Fact]
    public void MissingUrlIsNamed()
    {
        SignInPage.HostAndPath(null).ShouldBe("(none)");
    }
}
