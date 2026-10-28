using AoCEditor.Core.Diagnostics.Application;
using AoCEditor.Infrastructure.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Shouldly;
using Xunit;

namespace AoCEditor.Infrastructure.Tests.Diagnostics;

public sealed class FileLogSinkTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "aoc-diag-" + Guid.NewGuid().ToString("N"), "logs");
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 3, 1, 12, 0, 0, TimeSpan.Zero));

    public void Dispose() => Directory.Delete(Path.GetDirectoryName(_dir)!, recursive: true);

    private static LogEntry Entry(string message) => new(new DateTimeOffset(2026, 3, 1, 12, 0, 0, TimeSpan.Zero), LogLevel.Information, "Gateway", message);

    [Fact]
    public void WritesOneLinePerEntryAndCreatesTheFolder()
    {
        var sink = new FileLogSink(_dir, _time, 10_000, 5, TimeSpan.FromDays(7));

        sink.Write(Entry("hello"));
        sink.Write(Entry("world"));

        var lines = File.ReadAllLines(Path.Combine(_dir, FileLogSink.CurrentFileName));
        lines.Length.ShouldBe(2);
        lines[0].ShouldContain("hello");
        lines[0].ShouldContain("Information");
        lines[0].ShouldContain("Gateway");
    }

    [Fact]
    public void MultilineMessagesStayOnTheirOwnIndentedLines()
    {
        var sink = new FileLogSink(_dir, _time, 10_000, 5, TimeSpan.FromDays(7));

        sink.Write(Entry("first\nsecond"));

        File.ReadAllLines(Path.Combine(_dir, FileLogSink.CurrentFileName))[1].ShouldBe("    second");
    }

    [Fact]
    public void RollsWhenTheFileIsFullAndKeepsOnlyTheNewestFiles()
    {
        var sink = new FileLogSink(_dir, _time, 200, 3, TimeSpan.FromDays(7));

        for (var i = 0; i < 60; i++)
        {
            _time.Advance(TimeSpan.FromSeconds(1));
            sink.Write(Entry(new string('x', 50)));
        }

        var files = Directory.GetFiles(_dir);
        files.Length.ShouldBeLessThanOrEqualTo(3);
        files.Length.ShouldBeGreaterThan(1);
        files.ShouldAllBe(f => new FileInfo(f).Length <= 400);
    }

    [Fact]
    public void RemovesFilesOlderThanTheAgeLimit()
    {
        Directory.CreateDirectory(_dir);
        var old = Path.Combine(_dir, "aoceditor-old.log");
        File.WriteAllText(old, "old");
        File.SetLastWriteTimeUtc(old, _time.GetUtcNow().UtcDateTime - TimeSpan.FromDays(8));
        var fresh = Path.Combine(_dir, "aoceditor-fresh.log");
        File.WriteAllText(fresh, "fresh");
        File.SetLastWriteTimeUtc(fresh, _time.GetUtcNow().UtcDateTime - TimeSpan.FromDays(1));

        _ = new FileLogSink(_dir, _time, 10_000, 5, TimeSpan.FromDays(7));

        File.Exists(old).ShouldBeFalse();
        File.Exists(fresh).ShouldBeTrue();
    }

    [Fact]
    public void LeavesUnrelatedFilesAlone()
    {
        Directory.CreateDirectory(_dir);
        var other = Path.Combine(_dir, "notes.txt");
        File.WriteAllText(other, "mine");
        File.SetLastWriteTimeUtc(other, _time.GetUtcNow().UtcDateTime - TimeSpan.FromDays(100));

        _ = new FileLogSink(_dir, _time, 10_000, 5, TimeSpan.FromDays(7));

        File.Exists(other).ShouldBeTrue();
    }
}
