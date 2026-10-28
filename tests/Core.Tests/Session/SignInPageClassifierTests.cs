using AoCEditor.Core.Session.Application;
using Shouldly;
using Xunit;

namespace AoCEditor.Core.Tests.Session;

public sealed class SignInPageClassifierTests
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
        var candidate = SignInPageClassifier.Candidate(url is null ? null : new Uri(url), finished, cookie, Host);

        candidate.ShouldBe(expected);
    }
}
