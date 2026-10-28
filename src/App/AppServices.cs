using Diagnostics.Application;
using Gateway;
using Microsoft.Extensions.Logging;
using Session.Application;
using Session.Infrastructure;
using Session.UI;
using Shared.Contracts;
using Storage.Infrastructure;
using Sync.Application;

namespace AoCEditor;

/// <summary>Composition root: builds the object graph by hand.</summary>
public sealed class AppServices : IDisposable
{
    private readonly AocGateway _gateway;
    private readonly WebViewSignInBrowser _browser;
    private readonly SignInCoordinator _signIn;
    private readonly SqliteConnectionFactory _connections;

    public AppServices(ISecretStore? secrets = null, IAppDataPaths? paths = null, DebugMode? debug = null)
    {
        Paths = paths ?? new AppDataPaths();
        Diagnostics = DiagnosticsServices.Create(
            Paths.Root,
            debug ?? DebugMode.FromLaunch(Environment.GetCommandLineArgs(), Environment.GetEnvironmentVariable),
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            TimeProvider.System);
        var logs = Diagnostics.LoggerFactory;
        _connections = new SqliteConnectionFactory(Paths, logs.CreateLogger("Storage.Connection"));
        SyncState = new SqliteSyncStateStore(_connections);
        Settings = new SqliteKeyValueStore(_connections);
        Diagnostics.Debug.AttachStore(Settings);
        StorageStatus = new SqliteStorageStatus(_connections, Paths);
        var tokens = new SessionTokenProvider(secrets ?? SecretStores.CreateForCurrentOs());
        _gateway = new AocGateway(tokens, new FileResponseCache(Paths.Cache), events: Events, logger: logs.CreateLogger("Gateway.AocGateway"), requests: Diagnostics.Requests);
        _browser = new WebViewSignInBrowser(SignInUri, Path.Combine(Paths.Root, "webview"), logs.CreateLogger("Session.WebView"));
        _signIn = new SignInCoordinator(tokens, _gateway, () => _browser.TryClearProfile(), logger: logs.CreateLogger("Session.SignIn"));
        Sync = new SyncService(_gateway, SyncState, TimeProvider.System, logs.CreateLogger("Sync.SyncService"));
        logs.CreateLogger("App").AppStarted(Shared.AppInfo.Version, Diagnostics.Debug.IsEnabled);
    }

    public event EventHandler? SignedIn;

    public Uri SignInUri { get; } = new(new GatewayOptions().BaseAddress, "auth/login");

    public IAppDataPaths Paths { get; }

    public SessionEvents Events { get; } = new();

    public ISyncStateStore SyncState { get; }

    public DiagnosticsServices Diagnostics { get; }

    public IKeyValueStore Settings { get; }

    public IStorageStatus StorageStatus { get; }

    public SyncService Sync { get; }

    /// <summary>Opens (and so migrates) the database.</summary>
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await using var connection = await _connections.OpenAsync(cancellationToken);
        await Diagnostics.Debug.LoadAsync(Settings, cancellationToken);
    }

    public async Task<DiagnosticsSnapshot> SnapshotAsync(CancellationToken cancellationToken)
    {
        var session = await GetStatusAsync(cancellationToken);
        return new DiagnosticsSnapshot(
            Shared.AppInfo.Version,
            System.Runtime.InteropServices.RuntimeInformation.OSDescription + " " + System.Runtime.InteropServices.RuntimeInformation.OSArchitecture,
            System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
            session.State == SessionState.SignedIn ? "Signed in" : "Signed out",
            await StorageStatus.GetAsync(cancellationToken),
            Diagnostics.Requests.Recent,
            Diagnostics.Buffer.Snapshot());
    }

    public Task<SessionStatus> GetStatusAsync(CancellationToken cancellationToken) => _signIn.GetStatusAsync(cancellationToken);

    public Task<SignInResult> SignInWithBrowserAsync(CancellationToken cancellationToken) =>
        Announce(_signIn.SignInWithBrowserAsync(_browser, cancellationToken));

    public Task<SignInResult> SignInWithPastedTokenAsync(string token, CancellationToken cancellationToken) =>
        Announce(_signIn.SignInWithPastedTokenAsync(token, cancellationToken));

    public Task<SecretResult> SignOutAsync(CancellationToken cancellationToken) => _signIn.SignOutAsync(cancellationToken);

    public void Dispose()
    {
        _gateway.Dispose();
        Diagnostics.Dispose();
    }

    private async Task<SignInResult> Announce(Task<SignInResult> signIn)
    {
        var result = await signIn;
        if (result is SignInResult.SignedIn)
        {
            SignedIn?.Invoke(this, EventArgs.Empty);
        }

        return result;
    }
}

internal static partial class AppLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = "AoCEditor {Version} started, debug mode {Debug}")]
    public static partial void AppStarted(this ILogger logger, string version, bool debug);
}
