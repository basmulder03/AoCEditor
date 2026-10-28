using AoCEditor.Core.Diagnostics.Application;
using AoCEditor.Infrastructure.Storage;
using Shared.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;

namespace AoCEditor.UI.Tests;

// The console provider binds to the process-wide stderr at creation; other tests may write there too, so assert on unique text.
public sealed class DiagnosticsRegistrationTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "aoc-diag-" + Guid.NewGuid().ToString("N"));
    private readonly TextWriter _originalError = Console.Error;
    private readonly StringWriter _error = new();
    private ServiceProvider? _provider;

    public DiagnosticsRegistrationTests() => Console.SetError(_error);

    public void Dispose()
    {
        _provider?.Dispose();
        Console.SetError(_originalError);
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private ServiceProvider Build(DebugMode mode) => _provider = new ServiceCollection()
        .AddSingleton(TimeProvider.System)
        .AddSingleton<IAppDataPaths>(new AppDataPaths(_root))
        .AddDiagnostics(mode)
        .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

    private static DebugMode Off() => DebugMode.FromLaunch([], _ => null);

    private static DebugMode On() => DebugMode.FromLaunch(["--debug"], _ => null);

    [Fact]
    public void EveryRegisteredProviderRedactsBeforeItWrites()
    {
        var providers = Build(Off()).GetServices<ILoggerProvider>().ToList();

        providers.Count.ShouldBeGreaterThanOrEqualTo(3);
        providers.ShouldAllBe(p => p is RedactingLoggerProvider);
    }

    [Fact]
    public void LogsReachTheBufferAndTheFileRedacted()
    {
        var provider = Build(Off());

        provider.GetRequiredService<ILoggerFactory>().CreateLogger("Gateway.AocGateway").Say(LogLevel.Information, "hit session=abc123");

        provider.GetRequiredService<DiagnosticsView>().Buffer.Snapshot().Single().Message.ShouldBe("hit session=[redacted]");
        provider.Dispose();
        var file = File.ReadAllText(Path.Combine(_root, "logs", "aoceditor.log"));
        file.ShouldContain("hit session=[redacted]");
        file.ShouldNotContain("abc123");
    }

    [Fact]
    public void DebugMessagesAreDroppedUnlessDebugModeIsOn()
    {
        var quiet = Build(Off());
        var logger = quiet.GetRequiredService<ILoggerFactory>().CreateLogger("Gateway.AocGateway");
        logger.Say(LogLevel.Debug, "hidden");
        logger.IsEnabled(LogLevel.Debug).ShouldBeFalse();
        quiet.GetRequiredService<DiagnosticsView>().Buffer.Snapshot().ShouldBeEmpty();
        quiet.Dispose();

        var loud = Build(On());
        loud.GetRequiredService<ILoggerFactory>().CreateLogger("Gateway.AocGateway").Say(LogLevel.Debug, "shown");
        loud.GetRequiredService<DiagnosticsView>().Buffer.Snapshot().Single().Level.ShouldBe(LogLevel.Debug);
    }

    [Fact]
    public async Task ToggleTakesEffectWithoutRestart()
    {
        var provider = Build(Off());
        var view = provider.GetRequiredService<DiagnosticsView>();
        await view.Debug.LoadAsync(new MemoryStore(), TestContext.Current.CancellationToken);
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("Gateway.AocGateway");
        logger.IsEnabled(LogLevel.Debug).ShouldBeFalse();

        await view.Debug.SetAsync(true, TestContext.Current.CancellationToken);

        logger.IsEnabled(LogLevel.Debug).ShouldBeTrue();
    }

    [Fact]
    public void ConsoleIsOnlyUsedInDebugMode()
    {
        var quiet = Build(Off());
        quiet.GetRequiredService<ILoggerFactory>().CreateLogger("Gateway.AocGateway").Say(LogLevel.Information, "quiet");
        quiet.Dispose();
        _error.ToString().ShouldNotContain("quiet");

        var loud = Build(On());
        loud.GetRequiredService<ILoggerFactory>().CreateLogger("Gateway.AocGateway").Say(LogLevel.Information, "loud session=abc123");
        loud.Dispose();
        var text = _error.ToString();
        text.ShouldContain("loud session=[redacted]");
        text.ShouldNotContain("abc123");
    }

    private sealed class MemoryStore : IKeyValueStore
    {
        public Task<string?> GetAsync(string key, CancellationToken cancellationToken) => Task.FromResult<string?>(null);

        public Task SetAsync(string key, string value, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
