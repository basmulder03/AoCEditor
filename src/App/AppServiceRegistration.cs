using AoCEditor.Core.Diagnostics.Application;
using AoCEditor.Core.Gateway;
using AoCEditor.Core.Plugins.Application;
using AoCEditor.Core.Plugins.Domain;
using AoCEditor.Core.Puzzle.Application;
using AoCEditor.Core.Session.Application;
using AoCEditor.Core.Sync.Application;
using AoCEditor.Infrastructure.Diagnostics;
using AoCEditor.Infrastructure.Gateway;
using AoCEditor.Infrastructure.Plugins;
using AoCEditor.Infrastructure.Runner;
using AoCEditor.Infrastructure.Session;
using AoCEditor.Infrastructure.Storage;
using AoCEditor.Infrastructure.Workspace;
using AoCEditor.UI.Session;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Contracts;

namespace AoCEditor;

public static class AppServiceRegistration
{
    /// <summary>Registers every service explicitly; slices are created by factories, never by activation.</summary>
    public static IServiceCollection AddAocEditor(this IServiceCollection services, AppOverrides? overrides = null)
    {
        overrides ??= new AppOverrides();
        return services
            .AddPlatform(overrides)
            .AddDiagnostics(overrides)
            .AddStorage()
            .AddGatewayAndSession(overrides)
            .AddSlices()
            .AddShell();
    }

    private static IServiceCollection AddPlatform(this IServiceCollection services, AppOverrides overrides) => services
        .AddSingleton(TimeProvider.System)
        .AddSingleton<IAppDataPaths>(_ => overrides.Paths ?? new AppDataPaths());

    private static IServiceCollection AddDiagnostics(this IServiceCollection services, AppOverrides overrides) => services
        .AddSingleton(_ => overrides.Debug ?? DebugMode.FromLaunch(Environment.GetCommandLineArgs(), Environment.GetEnvironmentVariable))
        .AddSingleton(sp => DiagnosticsServices.Create(
            sp.GetRequiredService<IAppDataPaths>().Root,
            sp.GetRequiredService<DebugMode>(),
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            sp.GetRequiredService<TimeProvider>()))
        .AddSingleton(sp => sp.GetRequiredService<DiagnosticsServices>().LoggerFactory);

    private static IServiceCollection AddStorage(this IServiceCollection services) => services
        .AddSingleton<IConnectionFactory>(sp => new SqliteConnectionFactory(
            sp.GetRequiredService<IAppDataPaths>(), sp.GetRequiredService<ILoggerFactory>().CreateLogger("Storage.Connection")))
        .AddSingleton<ISyncStateStore>(sp => new SqliteSyncStateStore(sp.GetRequiredService<IConnectionFactory>()))
        .AddSingleton<IAnswerStore>(sp => new SqliteAnswerStore(sp.GetRequiredService<IConnectionFactory>()))
        .AddSingleton<IKeyValueStore>(sp => new SqliteKeyValueStore(sp.GetRequiredService<IConnectionFactory>()))
        .AddSingleton<IStorageStatus>(sp => new SqliteStorageStatus(sp.GetRequiredService<IConnectionFactory>(), sp.GetRequiredService<IAppDataPaths>()));

    private static IServiceCollection AddGatewayAndSession(this IServiceCollection services, AppOverrides overrides) => services
        .AddSingleton(sp => new SessionTokenProvider(overrides.Secrets ?? SecretStores.CreateForCurrentOs()))
        .AddSingleton<SessionEvents>()
        .AddSingleton<AocGateway>(sp => new AocGateway(
            sp.GetRequiredService<SessionTokenProvider>(),
            new FileResponseCache(sp.GetRequiredService<IAppDataPaths>().Cache),
            events: sp.GetRequiredService<SessionEvents>(),
            logger: sp.GetRequiredService<ILoggerFactory>().CreateLogger("Gateway.AocGateway"),
            requests: sp.GetRequiredService<DiagnosticsServices>().Requests))
        .AddSingleton<IAocGateway>(sp => sp.GetRequiredService<AocGateway>())
        .AddSingleton(sp => new WebViewSignInBrowser(
            new Uri(new GatewayOptions().BaseAddress, "auth/login"),
            Path.Combine(sp.GetRequiredService<IAppDataPaths>().Root, "webview"),
            sp.GetRequiredService<ILoggerFactory>().CreateLogger("Session.WebView")))
        .AddSingleton(sp =>
        {
            var browser = sp.GetRequiredService<WebViewSignInBrowser>();
            return new SignInCoordinator(
                sp.GetRequiredService<SessionTokenProvider>(),
                sp.GetRequiredService<IAocGateway>(),
                () => browser.TryClearProfile(),
                logger: sp.GetRequiredService<ILoggerFactory>().CreateLogger("Session.SignIn"));
        });

    private static IServiceCollection AddSlices(this IServiceCollection services) => services
        .AddSingleton(sp => new SyncService(
            sp.GetRequiredService<IAocGateway>(),
            sp.GetRequiredService<ISyncStateStore>(),
            sp.GetRequiredService<TimeProvider>(),
            sp.GetRequiredService<ILoggerFactory>().CreateLogger("Sync.SyncService")))
        .AddSingleton(sp => new PuzzleService(sp.GetRequiredService<IAocGateway>(), sp.GetRequiredService<IAnswerStore>(), sp.GetRequiredService<TimeProvider>()))
        .AddSingleton(sp => new WorkspaceService(sp.GetRequiredService<IKeyValueStore>(), sp.GetRequiredService<IAppDataPaths>()))
        .AddSingleton<IReadOnlyList<PluginManifest>>(_ => BuiltInPlugins.Load())
        .AddSingleton(sp =>
        {
            var paths = sp.GetRequiredService<IAppDataPaths>();
            var puzzle = sp.GetRequiredService<PuzzleService>();
            return new DayRunCoordinator(
                sp.GetRequiredService<WorkspaceService>(),
                (year, day, ct) => puzzle.LoadInputAsync(year, day, false, ct),
                new ProcessRunner([paths.Root]),
                paths,
                plugin => ToolchainLocator.Find(plugin.ToolchainExecutables, Environment.GetEnvironmentVariable("PATH")));
        });

    private static IServiceCollection AddShell(this IServiceCollection services) => services
        .AddSingleton(sp => new AppServices(
            sp.GetRequiredService<IAppDataPaths>(),
            sp.GetRequiredService<SessionEvents>(),
            sp.GetRequiredService<DiagnosticsServices>(),
            sp.GetRequiredService<IConnectionFactory>(),
            sp.GetRequiredService<ISyncStateStore>(),
            sp.GetRequiredService<IAnswerStore>(),
            sp.GetRequiredService<IKeyValueStore>(),
            sp.GetRequiredService<IStorageStatus>(),
            sp.GetRequiredService<WebViewSignInBrowser>(),
            sp.GetRequiredService<SignInCoordinator>(),
            sp.GetRequiredService<SyncService>(),
            sp.GetRequiredService<PuzzleService>(),
            sp.GetRequiredService<WorkspaceService>(),
            sp.GetRequiredService<IReadOnlyList<PluginManifest>>(),
            sp.GetRequiredService<DayRunCoordinator>()))
        .AddTransient(sp => new ShellWindow(sp.GetRequiredService<AppServices>()));
}
