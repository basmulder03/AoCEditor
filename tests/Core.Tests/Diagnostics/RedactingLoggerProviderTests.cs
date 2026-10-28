using AoCEditor.Core.Diagnostics.Application;
using AoCEditor.Core.Diagnostics.Domain;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;

namespace AoCEditor.Core.Tests.Diagnostics;

public sealed class RedactingLoggerProviderTests : IDisposable
{
    private readonly ListProvider _inner = new();

    public void Dispose() => _inner.Dispose();

    private ILogger Logger(string? home = "/home/alice") =>
        new RedactingLoggerProvider(new Redactor(home), _inner).CreateLogger("AoCEditor.Core.Session.Application.SignInCoordinator");

    [Fact]
    public void EveryMessageIsRedactedBeforeItReachesTheInnerProvider()
    {
        Logger().Say(LogLevel.Information, "cookie session=abc123 at /home/alice/x");

        _inner.Entries.Single().Message.ShouldBe("cookie session=[redacted] at ~/x");
    }

    [Fact]
    public void ExceptionTextIsRedactedAndNotForwardedAsAnException()
    {
        Logger().Say(LogLevel.Error, "boom", new InvalidOperationException("failed for /home/alice/db session=abc123"));

        var entry = _inner.Entries.Single();
        entry.Message.ShouldContain("boom");
        entry.Message.ShouldContain("InvalidOperationException");
        entry.Message.ShouldNotContain("alice");
        entry.Message.ShouldNotContain("abc123");
        entry.Exception.ShouldBeNull();
        entry.Level.ShouldBe(LogLevel.Error);
    }

    [Fact]
    public void FailingProviderDoesNotBreakTheCallerOrTheOtherProviders()
    {
        var failing = new RedactingLoggerProvider(new Redactor(null), new ThrowingProvider()).CreateLogger("Gateway.AocGateway");
        var working = new RedactingLoggerProvider(new Redactor(null), _inner).CreateLogger("Gateway.AocGateway");

        Should.NotThrow(() => failing.Say(LogLevel.Information, "lost"));
        working.Say(LogLevel.Information, "still arrives");

        _inner.Entries.Single().Message.ShouldBe("still arrives");
    }
}
