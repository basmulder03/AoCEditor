using System.Net;
using AoCEditor.Core.Gateway;
using AoCEditor.Infrastructure.Gateway;
using Microsoft.Extensions.Time.Testing;
using Shared.Contracts;
using Shouldly;
using Xunit;

namespace AoCEditor.Infrastructure.Tests.Gateway;

public sealed class ObservabilityTests : IDisposable
{
    private const string Secret = "s3cr3t-token-value";
    private readonly FakeHandler _handler = new();
    private readonly FakeTimeProvider _time = new();
    private readonly CapturingLogger<AocGateway> _log = new();
    private readonly ListRequestLog _requests = new();
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _handler.Dispose();

    private AocGateway Create() => new(_handler, new FakeTokens(Secret), new MemoryCache(), _log, new GatewayOptions { InteractiveInterval = TimeSpan.FromSeconds(5) }, _time, null, _requests);

    [Fact]
    public async Task EachRequestIsRecordedWithMethodPathStatusAndTimings()
    {
        using var gateway = Create();

        await gateway.GetAsync("/2024/day/1", RequestKind.Interactive, FetchPolicy.Refresh, Ct);

        var entry = _requests.Recent.Single();
        entry.Method.ShouldBe("GET");
        entry.Path.ShouldBe("/2024/day/1");
        entry.Status.ShouldBe(200);
        entry.ThrottleWait.ShouldBe(TimeSpan.Zero);
    }

    [Fact]
    public async Task ThrottleWaitIsReported()
    {
        using var gateway = Create();
        await gateway.GetAsync("/a", RequestKind.Interactive, FetchPolicy.Refresh, Ct);

        var second = gateway.GetAsync("/b", RequestKind.Interactive, FetchPolicy.Refresh, Ct);
        await Task.Yield();
        _time.Advance(TimeSpan.FromSeconds(5));
        await second;
        _requests.Recent[1].ThrottleWait.ShouldBeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task LogsAndRequestLogNeverContainTheTokenOrHeaders()
    {
        using var gateway = Create();

        await gateway.GetAsync("/p", RequestKind.Interactive, FetchPolicy.Refresh, Ct);
        var check = gateway.CheckTokenAsync(Secret, Ct);
        await Task.Yield();
        _time.Advance(TimeSpan.FromSeconds(5));
        await check;

        string.Join('\n', _log.Lines).ShouldNotContain(Secret);
        _requests.Recent.ShouldAllBe(r => !r.Path.Contains(Secret, StringComparison.Ordinal));
        _log.Lines.ShouldContain(l => l.Contains("GET /p -> 200", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ServerFailureCarriesItsReason()
    {
        _handler.Respond = _ => new HttpResponseMessage(HttpStatusCode.InternalServerError);
        using var gateway = Create();

        var result = await gateway.GetAsync("/p", RequestKind.Interactive, FetchPolicy.Refresh, Ct);

        result.ShouldBeOfType<GatewayResult.NetworkError>().Reason.ShouldBe("HTTP 500");
        _log.Lines.ShouldContain(l => l.StartsWith("Information", StringComparison.Ordinal) && l.Contains("-> 500", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ConnectionFailureIsLoggedAsAWarningWithReason()
    {
        _handler.Respond = _ => throw new HttpRequestException(HttpRequestError.NameResolutionError);
        using var gateway = Create();

        var result = await gateway.GetAsync("/p", RequestKind.Interactive, FetchPolicy.Refresh, Ct);

        result.ShouldBeOfType<GatewayResult.NetworkError>().Reason.ShouldBe("NameResolutionError");
        _log.Lines.ShouldContain(l => l.StartsWith("Warning", StringComparison.Ordinal) && l.Contains("NameResolutionError", StringComparison.Ordinal));
        _requests.Recent.Single().Status.ShouldBeNull();
    }
}
