using System.Net;
using Microsoft.Extensions.Time.Testing;
using Shared.Contracts;
using Shouldly;
using Xunit;

namespace Gateway.Tests;

public sealed class AocGatewayTests : IDisposable
{
    private readonly FakeHandler _handler = new();
    private readonly MemoryCache _cache = new();
    private readonly FakeTimeProvider _time = new();
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _handler.Dispose();

    private AocGateway Create(string? token = "secret") => new(_handler, new FakeTokens(token), _cache, new GatewayOptions { MinInterval = TimeSpan.FromSeconds(5) }, _time);

    [Fact]
    public async Task RequestIdentifiesTheAppAndCarriesTheSessionCookie()
    {
        using var gateway = Create();

        await gateway.GetAsync("/2024/day/1/input", FetchPolicy.CacheFirst, Ct);

        var request = _handler.Requests.Single();
        request.Headers.UserAgent.ToString().ShouldContain("github.com/basmulder03/AoCEditor");
        request.Headers.UserAgent.ToString().ShouldStartWith("AoCEditor/");
        request.Headers.GetValues("Cookie").Single().ShouldBe("session=secret");
        request.RequestUri!.AbsoluteUri.ShouldBe("https://adventofcode.com/2024/day/1/input");
    }

    [Fact]
    public async Task CacheFirstSkipsTheNetworkOnAHit()
    {
        _cache.Items["/p"] = "cached";
        using var gateway = Create();

        var result = await gateway.GetAsync("/p", FetchPolicy.CacheFirst, Ct);

        result.ShouldBe(new GatewayResult.Ok("cached"));
        _handler.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task RefreshGoesToTheNetworkAndUpdatesTheCache()
    {
        _cache.Items["/p"] = "old";
        using var gateway = Create();

        var result = await gateway.GetAsync("/p", FetchPolicy.Refresh, Ct);

        result.ShouldBe(new GatewayResult.Ok("body"));
        _cache.Items["/p"].ShouldBe("body");
    }

    [Theory]
    [InlineData(HttpStatusCode.Found, typeof(GatewayResult.NotSignedIn))]
    [InlineData(HttpStatusCode.BadRequest, typeof(GatewayResult.NotSignedIn))]
    [InlineData(HttpStatusCode.TooManyRequests, typeof(GatewayResult.RateLimited))]
    [InlineData(HttpStatusCode.NotFound, typeof(GatewayResult.NotFound))]
    [InlineData(HttpStatusCode.InternalServerError, typeof(GatewayResult.NetworkError))]
    public async Task FailuresMapToTypedResultsAndAreNotCachedOrRetried(HttpStatusCode status, Type expected)
    {
        _handler.Respond = _ => new HttpResponseMessage(status);
        using var gateway = Create();

        var result = await gateway.GetAsync("/p", FetchPolicy.CacheFirst, Ct);

        result.ShouldBeOfType(expected);
        _cache.Items.ShouldBeEmpty();
        _handler.Requests.Count.ShouldBe(1);
    }

    [Fact]
    public async Task ConnectionFailureIsANetworkError()
    {
        _handler.Respond = _ => throw new HttpRequestException("down");
        using var gateway = Create();

        var result = await gateway.GetAsync("/p", FetchPolicy.CacheFirst, Ct);

        result.ShouldBeOfType<GatewayResult.NetworkError>();
    }

    [Fact]
    public async Task WithoutATokenNothingIsSent()
    {
        using var gateway = Create(token: null);

        var result = await gateway.GetAsync("/p", FetchPolicy.CacheFirst, Ct);

        result.ShouldBeOfType<GatewayResult.NotSignedIn>();
        _handler.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task PostSendsTheFormAndIsNeverCached()
    {
        string? sent = null;
        _handler.Respond = r =>
        {
            sent = r.Content!.ReadAsStringAsync(Ct).Result;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("ok") };
        };
        using var gateway = Create();

        var result = await gateway.PostFormAsync("/2024/day/1/answer", new Dictionary<string, string> { ["level"] = "1", ["answer"] = "42" }, Ct);

        result.ShouldBe(new GatewayResult.Ok("ok"));
        sent.ShouldBe("level=1&answer=42");
        _cache.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task SecondRequestWaitsForTheMinimumInterval()
    {
        using var gateway = Create();
        await gateway.GetAsync("/a", FetchPolicy.Refresh, Ct);

        var second = gateway.GetAsync("/b", FetchPolicy.Refresh, Ct);
        await Task.Yield();
        _handler.Requests.Count.ShouldBe(1);

        _time.Advance(TimeSpan.FromSeconds(5));
        await second;
        _handler.Requests.Count.ShouldBe(2);
    }

    [Fact]
    public async Task WaitingForTheIntervalCanBeCancelled()
    {
        using var gateway = Create();
        await gateway.GetAsync("/a", FetchPolicy.Refresh, Ct);
        using var cts = new CancellationTokenSource();

        var second = gateway.GetAsync("/b", FetchPolicy.Refresh, cts.Token);
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => second);
        _handler.Requests.Count.ShouldBe(1);
    }

    [Fact]
    public async Task ConcurrentRequestsAreSerialized()
    {
        var inFlight = 0;
        var max = 0;
        _handler.Respond = _ =>
        {
            max = Math.Max(max, Interlocked.Increment(ref inFlight));
            Interlocked.Decrement(ref inFlight);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("x") };
        };
        using var gateway = Create();

        var all = Task.WhenAll(gateway.GetAsync("/a", FetchPolicy.Refresh, Ct), gateway.GetAsync("/b", FetchPolicy.Refresh, Ct));
        while (!all.IsCompleted)
        {
            _time.Advance(TimeSpan.FromSeconds(5));
            await Task.Yield();
        }

        await all;
        max.ShouldBe(1);
        _handler.Requests.Count.ShouldBe(2);
    }
}
