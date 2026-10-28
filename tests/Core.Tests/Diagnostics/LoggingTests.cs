using AoCEditor.Core.Diagnostics.Application;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;

namespace AoCEditor.Core.Tests.Diagnostics;

public sealed class LoggingTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
    private readonly ListSink _sink = new();

    private ILogger Logger(DebugMode mode, string? home = "/home/alice") =>
        new DiagnosticsLoggerProvider(new AoCEditor.Core.Diagnostics.Domain.Redactor(home), mode, [_sink], new Microsoft.Extensions.Time.Testing.FakeTimeProvider(Now)).CreateLogger("AoCEditor.Core.Session.Application.SignInCoordinator");

    private static DebugMode Off() => DebugMode.FromLaunch([], _ => null);

    private static DebugMode On() => DebugMode.FromLaunch(["--debug"], _ => null);

    [Fact]
    public void EveryMessageIsRedactedBeforeItReachesASink()
    {
        var logger = Logger(Off());

        logger.Say(LogLevel.Information, "cookie session=abc123 at /home/alice/x");

        _sink.Entries.Single().Message.ShouldBe("cookie session=[redacted] at ~/x");
    }

    [Fact]
    public void ExceptionTextIsRedactedToo()
    {
        var logger = Logger(Off());

        logger.Say(LogLevel.Error, "boom", new InvalidOperationException("failed for /home/alice/db session=abc123"));

        var message = _sink.Entries.Single().Message;
        message.ShouldContain("boom");
        message.ShouldContain("InvalidOperationException");
        message.ShouldNotContain("alice");
        message.ShouldNotContain("abc123");
    }

    [Fact]
    public void DebugMessagesAreDroppedUnlessDebugModeIsOn()
    {
        var quiet = Logger(Off());
        quiet.Say(LogLevel.Debug, "hidden");
        quiet.IsEnabled(LogLevel.Debug).ShouldBeFalse();
        _sink.Entries.ShouldBeEmpty();

        var loud = Logger(On());
        loud.Say(LogLevel.Debug, "shown");
        _sink.Entries.Single().Level.ShouldBe(LogLevel.Debug);
    }

    [Fact]
    public async Task ToggleTakesEffectWithoutRestart()
    {
        var mode = Off();
        mode.AttachStore(new MemoryStore());
        var logger = Logger(mode);

        await mode.SetAsync(true, TestContext.Current.CancellationToken);

        logger.IsEnabled(LogLevel.Debug).ShouldBeTrue();
    }

    [Fact]
    public void EntryCarriesTimeLevelAndShortSource()
    {
        Logger(Off()).Say(LogLevel.Warning, "careful");

        var entry = _sink.Entries.Single();
        entry.Time.ShouldBe(Now);
        entry.Level.ShouldBe(LogLevel.Warning);
        entry.Source.ShouldBe("Session");
    }

    [Fact]
    public void FailingSinkDoesNotBreakTheCaller()
    {
        var provider = new DiagnosticsLoggerProvider(new AoCEditor.Core.Diagnostics.Domain.Redactor(null), Off(), [new ThrowingSink(), _sink], TimeProvider.System);

        provider.CreateLogger("Gateway.AocGateway").Say(LogLevel.Information, "still arrives");

        _sink.Entries.Single().Message.ShouldBe("still arrives");
    }

    private sealed class ThrowingSink : ILogSink
    {
        public void Write(LogEntry entry) => throw new IOException("disk full");
    }
}
