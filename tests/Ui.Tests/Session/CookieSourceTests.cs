using Session.UI;
using Shouldly;
using Xunit;

namespace Session.Tests;

public sealed class CookieSourceTests
{
    private sealed class FakeSource(Func<CancellationToken, Task<CookieRead>> read) : ICookieSource
    {
        public Task<CookieRead> ReadAsync(Uri site, CancellationToken cancellationToken) => read(cancellationToken);

        public Task DeleteAsync(Uri site, string name, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    [Theory]
    [InlineData("session", "adventofcode.com", "adventofcode.com", true)]
    [InlineData("session", ".adventofcode.com", "adventofcode.com", true)]
    [InlineData("session", "ADVENTOFCODE.com", "adventofcode.com", true)]
    [InlineData("session", "github.com", "adventofcode.com", false)]
    [InlineData("other", "adventofcode.com", "adventofcode.com", false)]
    public void FindMatchesNameAndDomain(string name, string domain, string host, bool expected)
    {
        var found = CookieSources.Find([new BrowserCookie(name, domain, "v")], "session", host);

        (found is not null).ShouldBe(expected);
    }

    [Fact]
    public void ChoosePrefersTheFirstAvailableSource()
    {
        var first = new FakeSource(_ => Task.FromResult<CookieRead>(new CookieRead.Cookies([])));
        var second = new FakeSource(_ => Task.FromResult<CookieRead>(new CookieRead.Cookies([])));

        CookieSources.Choose(() => first, () => second).ShouldBeSameAs(first);
        CookieSources.Choose(() => null, () => second).ShouldBeSameAs(second);
        CookieSources.Choose(() => null, () => null).ShouldBeNull();
    }

    [Fact]
    public async Task GuardReportsUnsupportedWhenTheSourceNeverAnswers()
    {
        var never = new FakeSource(_ => new TaskCompletionSource<CookieRead>().Task);

        var result = await CookieSources.Guard(ct => never.ReadAsync(new Uri("https://x.test/"), ct), TimeSpan.FromMilliseconds(50), CancellationToken.None);

        result.ShouldBeOfType<CookieRead.Unsupported>().Reason.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task GuardPassesAPromptAnswerThrough()
    {
        var quick = new FakeSource(_ => Task.FromResult<CookieRead>(new CookieRead.Cookies([new BrowserCookie("a", "x", "v")])));

        var result = await CookieSources.Guard(ct => quick.ReadAsync(new Uri("https://x.test/"), ct), TimeSpan.FromSeconds(5), CancellationToken.None);

        result.ShouldBeOfType<CookieRead.Cookies>().Items.Count.ShouldBe(1);
    }

    [Fact]
    public async Task LinuxSourceWithoutAViewIsUnsupportedNotAnException()
    {
        var result = await new LinuxCookieSource(IntPtr.Zero).ReadAsync(new Uri("https://x.test/"), CancellationToken.None);

        result.ShouldBeOfType<CookieRead.Unsupported>();
    }
}
