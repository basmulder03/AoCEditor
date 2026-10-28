using AoCEditor.Core.Session.Application;
using Shouldly;
using Xunit;

namespace AoCEditor.Core.Tests.Session;

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
