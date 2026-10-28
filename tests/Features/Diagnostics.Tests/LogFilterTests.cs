using Diagnostics.Application;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;

namespace Diagnostics.Tests;

public sealed class LogFilterTests
{
    private static readonly LogEntry[] Entries =
    [
        new(DateTimeOffset.UnixEpoch, LogLevel.Debug, "Gateway", "d"),
        new(DateTimeOffset.UnixEpoch, LogLevel.Information, "Session", "i"),
        new(DateTimeOffset.UnixEpoch, LogLevel.Warning, "Gateway", "w"),
        new(DateTimeOffset.UnixEpoch, LogLevel.Error, "Storage", "e"),
    ];

    [Fact]
    public void NoFilterKeepsEverythingNewestFirst()
    {
        LogFilter.Apply(Entries, null, null).Select(e => e.Message).ToArray().ShouldBe(["e", "w", "i", "d"]);
    }

    [Fact]
    public void LevelIsAMinimum()
    {
        LogFilter.Apply(Entries, LogLevel.Warning, null).Select(e => e.Message).ToArray().ShouldBe(["e", "w"]);
    }

    [Fact]
    public void SourceMatchesExactly()
    {
        LogFilter.Apply(Entries, null, "Gateway").Select(e => e.Message).ToArray().ShouldBe(["w", "d"]);
    }

    [Fact]
    public void SourcesAreDistinctAndSorted()
    {
        LogFilter.Sources(Entries).ShouldBe(["Gateway", "Session", "Storage"]);
    }

    [Fact]
    public void LineHasTimeLevelSourceAndMessage()
    {
        LogFilter.Line(Entries[1]).ShouldBe("00:00:00.000 Information Session i");
    }
}
