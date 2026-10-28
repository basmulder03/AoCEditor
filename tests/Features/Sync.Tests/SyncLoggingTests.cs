using Microsoft.Extensions.Time.Testing;
using Shared.Contracts;
using Shouldly;
using Sync.Application;
using Xunit;

namespace Sync.Tests;

public class SyncLoggingTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task AFailedYearKeepsItsReasonInTheSummaryAndTheLog()
    {
        var gateway = new FakeGateway();
        gateway.Pages["/events"] = new GatewayResult.Ok(Html.Events("/2024"));
        gateway.Pages["/2024"] = new GatewayResult.NetworkError("HTTP 500");
        var log = new CapturingLogger();
        var service = new SyncService(gateway, new MemoryStore(), new FakeTimeProvider(), log);

        var summary = await service.SyncAllAsync([], new Progress<SyncProgress>(), Ct);

        summary.Errors.ShouldBe([new YearError(2024, SyncError.NetworkError, "HTTP 500")]);
        log.Lines.ShouldContain(l => l.StartsWith("Warning", StringComparison.Ordinal) && l.Contains("HTTP 500", StringComparison.Ordinal));
    }

    [Fact]
    public async Task UnavailableEventListIsLoggedWithItsReason()
    {
        var gateway = new FakeGateway();
        gateway.Pages["/events"] = new GatewayResult.NetworkError("NameResolutionError");
        var log = new CapturingLogger();

        await new SyncService(gateway, new MemoryStore(), new FakeTimeProvider(), log).SyncAllAsync([], new Progress<SyncProgress>(), Ct);

        log.Lines.ShouldContain(l => l.Contains("NameResolutionError", StringComparison.Ordinal));
    }
}
