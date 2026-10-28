using Diagnostics.Application;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;

namespace Diagnostics.Tests;

public sealed class LogBufferTests
{
    private static LogEntry Entry(string message) => new(DateTimeOffset.UnixEpoch, LogLevel.Information, "Gateway", message);

    [Fact]
    public void KeepsOnlyTheNewestEntries()
    {
        var buffer = new LogBuffer(3);

        foreach (var i in Enumerable.Range(1, 5))
        {
            buffer.Write(Entry($"m{i}"));
        }

        buffer.Snapshot().Select(e => e.Message).ToArray().ShouldBe(["m3", "m4", "m5"]);
    }

    [Fact]
    public void RaisesChangedOnEveryWrite()
    {
        var buffer = new LogBuffer(3);
        var raised = 0;
        buffer.Changed += (_, _) => raised++;

        buffer.Write(Entry("a"));
        buffer.Write(Entry("b"));

        raised.ShouldBe(2);
    }

    [Fact]
    public void SnapshotIsACopy()
    {
        var buffer = new LogBuffer(3);
        buffer.Write(Entry("a"));
        var snapshot = buffer.Snapshot();

        buffer.Write(Entry("b"));

        snapshot.Count.ShouldBe(1);
    }

    [Fact]
    public async Task SurvivesConcurrentWriters()
    {
        var buffer = new LogBuffer(50);

        await Task.WhenAll(Enumerable.Range(0, 8).Select(t => Task.Run(() =>
        {
            for (var i = 0; i < 500; i++)
            {
                buffer.Write(Entry($"{t}-{i}"));
            }
        }, TestContext.Current.CancellationToken)));

        buffer.Snapshot().Count.ShouldBe(50);
    }
}
