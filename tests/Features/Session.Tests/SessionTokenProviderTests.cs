using Session.Application;
using Shouldly;
using Xunit;

namespace Session.Tests;

public sealed class SessionTokenProviderTests
{
    private const string Token = "synthetic-token-123";

    [Fact]
    public async Task ReturnsNullWhenNothingIsStored()
    {
        var provider = new SessionTokenProvider(new FakeSecretStore());

        var token = await provider.GetAsync(TestContext.Current.CancellationToken);

        token.ShouldBeNull();
    }

    [Fact]
    public async Task ReturnsTheSavedTokenTrimmed()
    {
        var provider = new SessionTokenProvider(new FakeSecretStore());

        await provider.SaveAsync("  " + Token + "\n", TestContext.Current.CancellationToken);
        var token = await provider.GetAsync(TestContext.Current.CancellationToken);

        token.ShouldBe(Token);
    }

    [Fact]
    public async Task ClearRemovesTheTokenAndSucceedsWhenAlreadyEmpty()
    {
        var provider = new SessionTokenProvider(new FakeSecretStore());
        await provider.SaveAsync(Token, TestContext.Current.CancellationToken);

        var first = await provider.ClearAsync(TestContext.Current.CancellationToken);
        var second = await provider.ClearAsync(TestContext.Current.CancellationToken);

        first.ShouldBeOfType<SecretResult.Ok>();
        second.ShouldBeOfType<SecretResult.Ok>();
        (await provider.GetAsync(TestContext.Current.CancellationToken)).ShouldBeNull();
    }

    [Fact]
    public async Task UnavailableStoreYieldsNoTokenAndReportsTheReasonOnSave()
    {
        var provider = new SessionTokenProvider(new FakeSecretStore { Unavailable = true });

        var token = await provider.GetAsync(TestContext.Current.CancellationToken);
        var saved = await provider.SaveAsync(Token, TestContext.Current.CancellationToken);

        token.ShouldBeNull();
        saved.ShouldBeOfType<SecretResult.Unavailable>().Reason.ShouldBe("no service");
    }

    [Fact]
    public async Task EmptyTokenIsRejectedWithoutEchoingIt()
    {
        var provider = new SessionTokenProvider(new FakeSecretStore());

        var ex = await Should.ThrowAsync<ArgumentException>(() => provider.SaveAsync("   ", TestContext.Current.CancellationToken));

        ex.ParamName.ShouldBe("token");
    }

    [Fact]
    public void ResultTextDoesNotContainTheSecret()
    {
        new SecretResult.Ok(Token).ToString().ShouldNotContain(Token);
    }
}
