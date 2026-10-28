using AoCEditor.Core.Diagnostics.Domain;
using Shouldly;
using Xunit;

namespace AoCEditor.Core.Tests.Diagnostics;

public sealed class RedactorTests
{
    private const string Hex96 = "53616c7465645f5f0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";
    private static readonly Redactor Home = new("/home/alice");

    [Theory]
    [InlineData("sent session=abc123 ok")]
    [InlineData("SESSION=abc123")]
    [InlineData("Cookie: session=abc123; other=1")]
    [InlineData("Set-Cookie: session=abc123; HttpOnly; Secure")]
    [InlineData("cookie=abc123")]
    [InlineData("Authorization: Bearer abc123")]
    [InlineData("{\"session\":\"abc123\"}")]
    [InlineData("{ \"token\" : \"abc123\" }")]
    [InlineData("url?session=abc123&x=1")]
    [InlineData("token=abc123")]
    public void SessionAndCookieValuesNeverSurvive(string text)
    {
        var result = Home.Apply(text);

        result.ShouldNotContain("abc123");
        result.ShouldContain("[redacted]");
    }

    [Fact]
    public void TextAroundASessionPairIsKept()
    {
        Home.Apply("before session=abc123&x=1 after").ShouldBe("before session=[redacted]&x=1 after");
    }

    [Fact]
    public void LongHexStringsAreRedacted()
    {
        var result = Home.Apply($"value {Hex96} end");

        result.ShouldBe("value [redacted] end");
    }

    [Theory]
    [InlineData("deadbeef")]
    [InlineData("0123456789abcdef0123456789abcde")]
    [InlineData("2024")]
    [InlineData("a commit message")]
    public void ShortHexAndPlainTextAreLeftAlone(string text)
    {
        Home.Apply(text).ShouldBe(text);
    }

    [Theory]
    [InlineData("/home/alice/.local/share/AoCEditor/logs", "~/.local/share/AoCEditor/logs")]
    [InlineData("open /home/alice", "open ~")]
    [InlineData("a /home/alice/x and /home/alice/y", "a ~/x and ~/y")]
    [InlineData("/HOME/ALICE/x", "~/x")]
    [InlineData("/home/alicia/x", "/home/alicia/x")]
    [InlineData("/home/alice2", "/home/alice2")]
    public void HomeFolderBecomesTilde(string text, string expected)
    {
        Home.Apply(text).ShouldBe(expected);
    }

    [Fact]
    public void WindowsHomeIsRecognisedWithEitherSlash()
    {
        var windows = new Redactor(@"C:\Users\Alice");

        windows.Apply(@"C:\Users\Alice\AppData\x").ShouldBe(@"~\AppData\x");
        windows.Apply("C:/Users/Alice/AppData/x").ShouldBe("~/AppData/x");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("/")]
    public void UselessHomeValuesDoNotMangleText(string? home)
    {
        new Redactor(home).Apply("/usr/lib and /x").ShouldBe("/usr/lib and /x");
    }

    [Fact]
    public void SecretInsideAPathInsideAMessageIsStillRedacted()
    {
        var result = Home.Apply($"/home/alice/cache?session={Hex96}");

        result.ShouldBe("~/cache?session=[redacted]");
    }

    [Fact]
    public void RedactionIsIdempotent()
    {
        var once = Home.Apply("session=abc123 /home/alice");

        Home.Apply(once).ShouldBe(once);
    }

    [Fact]
    public void WordsThatMerelyMentionASessionAreKept()
    {
        Home.Apply("session state: signed in").ShouldBe("session state: signed in");
    }
}
