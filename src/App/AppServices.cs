using AoCEditor.Core.Diagnostics.Application;
using AoCEditor.Core.Gateway;
using AoCEditor.Core.Plugins.Domain;
using AoCEditor.Core.Puzzle.Application;
using AoCEditor.Core.Session.Application;
using AoCEditor.Core.Sync.Application;
using AoCEditor.Infrastructure.Diagnostics;
using AoCEditor.Infrastructure.Session;
using AoCEditor.Infrastructure.Storage;
using AoCEditor.Infrastructure.Workspace;
using AoCEditor.UI.Session;
using Microsoft.Extensions.Logging;
using Shared.Contracts;

namespace AoCEditor;

/// <summary>Application facade over the registered services; built by the container.</summary>
public sealed class AppServices
{
    private readonly WebViewSignInBrowser _browser;
    private readonly SignInCoordinator _signIn;
    private readonly IConnectionFactory _connections;

    public AppServices(
        IAppDataPaths paths,
        SessionEvents events,
        DiagnosticsServices diagnostics,
        IConnectionFactory connections,
        ISyncStateStore syncState,
        IAnswerStore answers,
        IKeyValueStore settings,
        IStorageStatus storageStatus,
        WebViewSignInBrowser browser,
        SignInCoordinator signIn,
        SyncService sync,
        PuzzleService puzzle,
        WorkspaceService workspace,
        IReadOnlyList<PluginManifest> plugins,
        DayRunCoordinator coordinator)
    {
        Paths = paths;
        Events = events;
        Diagnostics = diagnostics;
        _connections = connections;
        SyncState = syncState;
        Answers = answers;
        Settings = settings;
        StorageStatus = storageStatus;
        _browser = browser;
        _signIn = signIn;
        Sync = sync;
        Puzzle = puzzle;
        Workspace = workspace;
        Plugins = plugins;
        Coordinator = coordinator;
        Diagnostics.Debug.AttachStore(Settings);
        Diagnostics.LoggerFactory.CreateLogger("App").AppStarted(Shared.AppInfo.Version, Diagnostics.Debug.IsEnabled);
    }

    public event EventHandler? SignedIn;

    public Uri SignInUri { get; } = new(new GatewayOptions().BaseAddress, "auth/login");

    public IAppDataPaths Paths { get; }

    public SessionEvents Events { get; }

    public ISyncStateStore SyncState { get; }

    public IAnswerStore Answers { get; }

    public DiagnosticsServices Diagnostics { get; }

    public IKeyValueStore Settings { get; }

    public IStorageStatus StorageStatus { get; }

    public SyncService Sync { get; }

    public PuzzleService Puzzle { get; }

    public WorkspaceService Workspace { get; }

    public IReadOnlyList<PluginManifest> Plugins { get; }

    public DayRunCoordinator Coordinator { get; }

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
