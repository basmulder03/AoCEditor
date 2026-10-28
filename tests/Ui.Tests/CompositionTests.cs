using AoCEditor;
using AoCEditor.Core.Diagnostics.Application;
using AoCEditor.Core.Plugins.Domain;
using AoCEditor.Core.Puzzle.Application;
using AoCEditor.Core.Session.Application;
using AoCEditor.Core.Sync.Application;
using AoCEditor.Infrastructure.Gateway;
using AoCEditor.Infrastructure.Storage;
using AoCEditor.Infrastructure.Workspace;
using AoCEditor.UI.Session;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Contracts;
using Shouldly;
using Xunit;

namespace AoCEditor.UI.Tests;

public sealed class CompositionTests : IDisposable
{
    private readonly ServiceProvider _provider = new ServiceCollection()
        .AddAocEditor(new AppOverrides(
            new AccountTests.Store(),
            new AppDataPaths(Path.Combine(Path.GetTempPath(), "aoceditor-di-" + Guid.NewGuid().ToString("N"))),
            DebugMode.FromLaunch([], _ => null)))
        .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

    public void Dispose() => _provider.Dispose();

    public static TheoryData<Type> Registrations() =>
    [
        typeof(TimeProvider), typeof(IAppDataPaths), typeof(DebugMode), typeof(DiagnosticsView), typeof(ILoggerFactory),
        typeof(IConnectionFactory), typeof(ISyncStateStore), typeof(IAnswerStore), typeof(IKeyValueStore), typeof(IStorageStatus),
        typeof(SessionTokenProvider), typeof(SessionEvents), typeof(AocGateway), typeof(IAocGateway), typeof(WebViewSignInBrowser),
        typeof(SignInCoordinator), typeof(SyncService), typeof(PuzzleService), typeof(WorkspaceService),
        typeof(IReadOnlyList<PluginManifest>), typeof(DayRunCoordinator), typeof(AppServices),
    ];

    [Theory]
    [MemberData(nameof(Registrations))]
    public void EveryRegistrationResolves(Type service) => _provider.GetRequiredService(service).ShouldNotBeNull();

    [Theory]
    [MemberData(nameof(Registrations))]
    public void SharedStateIsOneInstance(Type service) =>
        _provider.GetRequiredService(service).ShouldBeSameAs(_provider.GetRequiredService(service));

    [Fact]
    public void EveryLoggerProviderRedactsBeforeItWrites() =>
        _provider.GetServices<ILoggerProvider>().ShouldAllBe(p => p is RedactingLoggerProvider);

    [Fact]
    public void GatewayAbstractionAndConcreteTypeAreTheSameInstance() =>
        _provider.GetRequiredService<IAocGateway>().ShouldBeSameAs(_provider.GetRequiredService<AocGateway>());

    [Fact]
    public void AppServicesExposesTheSharedSessionEvents() =>
        _provider.GetRequiredService<AppServices>().Events.ShouldBeSameAs(_provider.GetRequiredService<SessionEvents>());

    [Fact]
    public void NoSliceProjectReferencesTheContainer()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(root.FullName, "AoCEditor.slnx")))
        {
            root = root.Parent ?? throw new InvalidOperationException("Repository root not found.");
        }

        var projects = Directory.GetFiles(Path.Combine(root.FullName, "src"), "*.csproj", SearchOption.AllDirectories)
            .Where(p => !p.EndsWith(Path.Combine("App", "App.csproj"), StringComparison.Ordinal))
            .ToList();
        projects.ShouldNotBeEmpty();
        foreach (var project in projects)
        {
            File.ReadAllText(project).ShouldNotContain("Microsoft.Extensions.DependencyInjection", customMessage: project);
        }
    }
}
