using AoCEditor.Core.Diagnostics.Application;
using AoCEditor.Infrastructure.Diagnostics;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;

namespace AoCEditor.Infrastructure.Tests.Diagnostics;

public sealed class DiagnosticsServicesTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "aoc-diag-" + Guid.NewGuid().ToString("N"));

    public void Dispose() => Directory.Delete(_root, recursive: true);

    [Fact]
    public void LogsReachTheFileAndTheBufferRedacted()
    {
        using var services = DiagnosticsServices.Create(_root, DebugMode.FromLaunch([], _ => null), "/home/alice", TimeProvider.System);

        services.LoggerFactory.CreateLogger("Gateway.AocGateway").Say(LogLevel.Information, "hit session=abc123");

        services.Buffer.Snapshot().Single().Message.ShouldBe("hit session=[redacted]");
        var file = File.ReadAllText(Path.Combine(_root, "logs", "aoceditor.log"));
        file.ShouldContain("hit session=[redacted]");
        file.ShouldNotContain("abc123");
    }

    [Fact]
    public void ConsoleSinkIsOnlyUsedInDebugMode()
    {
        var console = new StringWriter();
        var mode = DebugMode.FromLaunch([], _ => null);
        using var services = DiagnosticsServices.Create(_root, mode, null, TimeProvider.System, console);
        var logger = services.LoggerFactory.CreateLogger("Gateway.AocGateway");

        logger.Say(LogLevel.Information, "quiet");
        console.ToString().ShouldBeEmpty();

        var debug = DebugMode.FromLaunch(["--debug"], _ => null);
        using var loud = DiagnosticsServices.Create(Path.Combine(_root, "b"), debug, null, TimeProvider.System, console);
        loud.LoggerFactory.CreateLogger("Gateway.AocGateway").Say(LogLevel.Information, "loud");
        console.ToString().ShouldContain("loud");
    }
}
