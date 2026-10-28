using Puzzle.Domain;
using Shouldly;
using Xunit;

namespace Puzzle.Tests;

public class LinkResolverTests
{
    private static readonly Uri Site = new("https://adventofcode.com/");

    [Theory]
    [InlineData("/2020/about", "https://adventofcode.com/2020/about")]
    [InlineData("https://en.wikipedia.org/wiki/Ant", "https://en.wikipedia.org/wiki/Ant")]
    [InlineData("http://example.com/a", "http://example.com/a")]
    public void WebLinksResolveToAbsoluteUris(string href, string expected)
    {
        LinkResolver.Resolve(Site, href)!.AbsoluteUri.ShouldBe(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("javascript:alert(1)")]
    [InlineData("file:///etc/passwd")]
    [InlineData("ms-settings:")]
    [InlineData("//evil.example/x")]
    public void NothingButHttpLinksIsOpenable(string? href)
    {
        LinkResolver.Resolve(Site, href).ShouldBeNull();
    }

    [Fact]
    public void WithoutASiteOnlyAbsoluteLinksResolve()
    {
        LinkResolver.Resolve(null, "/2020/about").ShouldBeNull();
        LinkResolver.Resolve(null, "https://example.com/").ShouldNotBeNull();
    }
}
