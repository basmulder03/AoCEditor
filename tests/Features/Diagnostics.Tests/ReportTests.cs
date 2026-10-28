using Diagnostics.Application;
using Diagnostics.Domain;
using Microsoft.Extensions.Logging;
using Shared.Contracts;
using Shouldly;
using Xunit;

namespace Diagnostics.Tests;

public sealed class ReportTests
{
    private static readonly Redactor Home = new("/home/alice");

    private static DiagnosticsSnapshot Snapshot(string session = "Signed in") => new(
        "1.2.3",
        "Linux 7 x64",
        ".NET 10.0.0",
        session,
        new StorageStatus("/home/alice/data/aoceditor.db", 1, 1),
        [new GatewayRequestEntry(DateTimeOffset.UnixEpoch, "GET", "/2024/day/1", 200, TimeSpan.FromMilliseconds(120), TimeSpan.FromSeconds(2))],
        [new LogEntry(DateTimeOffset.UnixEpoch, LogLevel.Warning, "Session", "cookie session=abc123 seen")]);

    [Fact]
    public void ReportListsVersionsStateRequestsAndLogs()
    {
        var text = DiagnosticsReport.Build(Snapshot(), Home);

        text.ShouldContain("App: 1.2.3");
        text.ShouldContain("OS: Linux 7 x64");
        text.ShouldContain("Runtime: .NET 10.0.0");
        text.ShouldContain("Session: Signed in");
        text.ShouldContain("Schema version: 1");
        text.ShouldContain("GET /2024/day/1 200 120 ms (throttle wait 2000 ms)");
        text.ShouldContain("Warning");
    }

    [Fact]
    public void ReportIsRedacted()
    {
        var text = DiagnosticsReport.Build(Snapshot(), Home);

        text.ShouldNotContain("abc123");
        text.ShouldNotContain("alice");
        text.ShouldContain("~/data/aoceditor.db");
    }

    [Fact]
    public void StorageErrorIsShown()
    {
        var snapshot = Snapshot() with { Storage = new StorageStatus("/db", 0, 0, "disk I/O error") };

        DiagnosticsReport.Build(snapshot, Home).ShouldContain("Error: disk I/O error");
    }
}
