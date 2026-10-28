using AoCEditor.Core.Diagnostics.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Shouldly;
using Xunit;

namespace AoCEditor.Core.Tests.Diagnostics;

public sealed class BufferLoggerProviderTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);

    private static BufferLoggerProvider Create(int capacity) => new(capacity, new FakeTimeProvider(Now));

    [Fact]
    public void KeepsOnlyTheNewestEntries()
    {
        var buffer = Create(3);
        var logger = buffer.CreateLogger("Gateway.AocGateway");

        foreach (var i in Enumerable.Range(1, 5))
        {
            logger.Say(LogLevel.Information, $"m{i}");
        }

        buffer.Snapshot().Select(e => e.Message).ToArray().ShouldBe(["m3", "m4", "m5"]);
    }

    [Fact]
    public void RaisesChangedOnEveryWrite()
    {
        var buffer = Create(3);
        var logger = buffer.CreateLogger("Gateway.AocGateway");
        var raised = 0;
        buffer.Changed += (_, _) => raised++;

        logger.Say(LogLevel.Information, "a");
        logger.Say(LogLevel.Information, "b");

        raised.ShouldBe(2);
    }

    [Fact]
    public void SnapshotIsACopy()
    {
        var buffer = Create(3);
        var logger = buffer.CreateLogger("Gateway.AocGateway");
        logger.Say(LogLevel.Information, "a");
        var snapshot = buffer.Snapshot();

        logger.Say(LogLevel.Information, "b");

        snapshot.Count.ShouldBe(1);
    }

    [Fact]
    public async Task SurvivesConcurrentWriters()
    {
        var buffer = Create(50);

        await Task.WhenAll(Enumerable.Range(0, 8).Select(t => Task.Run(() =>
        {
            var logger = buffer.CreateLogger("Gateway.AocGateway");
            for (var i = 0; i < 500; i++)
            {
                logger.Say(LogLevel.Information, $"{t}-{i}");
            }
        }, TestContext.Current.CancellationToken)));

        buffer.Snapshot().Count.ShouldBe(50);
    }

    [Fact]
    public void EntryCarriesTimeLevelAndSliceName()
    {
        var buffer = Create(3);

        buffer.CreateLogger("AoCEditor.Core.Session.Application.SignInService").Say(LogLevel.Warning, "careful");

        var entry = buffer.Snapshot().Single();
        entry.Time.ShouldBe(Now);
        entry.Level.ShouldBe(LogLevel.Warning);
        entry.Source.ShouldBe("Session");
    }

    [Theory]
    [InlineData("AoCEditor.Core.Session.Application.SignInService", "Session")]
    [InlineData("AoCEditor.Infrastructure.Gateway.AocGateway", "Gateway")]
    [InlineData("AoCEditor.UI.Sync.Calendar", "Sync")]
    [InlineData("Gateway.AocGateway", "Gateway")]
    [InlineData("AoCEditor.App", "AoCEditor")]
    [InlineData("AoCEditor.Other.Thing.Deep", "AoCEditor")]
    public void SourceIsTheSliceNameOrTheFirstSegment(string category, string expected) =>
        LogEntry.SourceOf(category).ShouldBe(expected);
}
