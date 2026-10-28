using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using AoCEditor.Core.Gateway;
using AoCEditor.Infrastructure.Gateway;
using Microsoft.Extensions.Time.Testing;
using Shared.Contracts;
using Shouldly;
using Xunit;

namespace AoCEditor.Infrastructure.Tests.Gateway;

public sealed class SignInEventTests : IDisposable
{
    private readonly FakeHandler _handler = new() { Respond = _ => new HttpResponseMessage(HttpStatusCode.Found) };
    private readonly FakeTimeProvider _time = new();
    private readonly FakeEvents _events = new();
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _handler.Dispose();

    private AocGateway Create(string? token = "secret") =>
        new(_handler, new FakeTokens(token), new MemoryCache(), NullLogger<AocGateway>.Instance, new GatewayOptions { InteractiveInterval = TimeSpan.FromSeconds(1) }, _time, _events);

    [Fact]
    public async Task RejectedTokenAsksForSignIn()
    {
        using var gateway = Create();

        await gateway.GetAsync("/p", RequestKind.Interactive, FetchPolicy.Refresh, Ct);

        _events.Raised.ShouldBe(1);
    }

    [Fact]
    public async Task MissingTokenAsksForSignIn()
    {
        using var gateway = Create(token: null);

        await gateway.GetAsync("/p", RequestKind.Interactive, FetchPolicy.Refresh, Ct);

        _events.Raised.ShouldBe(1);
    }

    [Fact]
    public async Task ABurstOfFailuresAsksOnce()
    {
        using var gateway = Create();

        for (var i = 0; i < 3; i++)
        {
            await gateway.GetAsync("/p", RequestKind.Interactive, FetchPolicy.Refresh, Ct);
            _time.Advance(TimeSpan.FromSeconds(1));
        }

        _events.Raised.ShouldBe(1);
    }

    [Fact]
    public async Task AFailureLongAfterTheLastPromptAsksAgain()
    {
        using var gateway = Create();
        await gateway.GetAsync("/p", RequestKind.Interactive, FetchPolicy.Refresh, Ct);

        _time.Advance(TimeSpan.FromMinutes(5));
        await gateway.GetAsync("/p", RequestKind.Interactive, FetchPolicy.Refresh, Ct);

        _events.Raised.ShouldBe(2);
    }

    [Fact]
    public async Task SuccessfulAndOtherFailedRequestsDoNotAsk()
    {
        _handler.Respond = _ => new HttpResponseMessage(HttpStatusCode.InternalServerError);
        using var gateway = Create();

        await gateway.GetAsync("/p", RequestKind.Interactive, FetchPolicy.Refresh, Ct);

        _events.Raised.ShouldBe(0);
    }

    [Fact]
    public async Task CheckingATokenSendsItWithoutStoringAndDoesNotAsk()
    {
        using var gateway = Create(token: null);

        var result = await gateway.CheckTokenAsync("candidate", Ct);

        result.ShouldBeOfType<GatewayResult.NotSignedIn>();
        _handler.Requests.Single().Headers.GetValues("Cookie").Single().ShouldBe("session=candidate");
        _handler.Requests.Single().RequestUri!.AbsolutePath.ShouldBe("/settings");
        _events.Raised.ShouldBe(0);
    }

    [Fact]
    public async Task CheckingAValidTokenIsOk()
    {
        _handler.Respond = _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("page") };
        using var gateway = Create(token: null);

        var result = await gateway.CheckTokenAsync("candidate", Ct);

        result.ShouldBeOfType<GatewayResult.Ok>();
    }

    private sealed class FakeEvents : ISessionEvents
    {
        public int Raised { get; private set; }

        public event EventHandler? SignInRequired;

        public void NotifySignInRequired()
        {
            Raised++;
            SignInRequired?.Invoke(this, EventArgs.Empty);
        }
    }
}
