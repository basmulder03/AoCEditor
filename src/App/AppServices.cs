using Gateway;
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

    public AppServices(ISecretStore? secrets = null, IAppDataPaths? paths = null)
    {
        Paths = paths ?? new AppDataPaths();
        _connections = new SqliteConnectionFactory(Paths);
        SyncState = new SqliteSyncStateStore(_connections);
        var tokens = new SessionTokenProvider(secrets ?? SecretStores.CreateForCurrentOs());
        _gateway = new AocGateway(tokens, new FileResponseCache(Paths.Cache), events: Events);
        _browser = new WebViewSignInBrowser(new Uri(new GatewayOptions().BaseAddress, "auth/login"), Path.Combine(Paths.Root, "webview"));
        _signIn = new SignInCoordinator(tokens, _gateway, () => _browser.TryClearProfile());
        Sync = new SyncService(_gateway, SyncState, TimeProvider.System);
    }

    public event EventHandler? SignedIn;

    public IAppDataPaths Paths { get; }

    public SessionEvents Events { get; } = new();

    public ISyncStateStore SyncState { get; }

    public SyncService Sync { get; }

    /// <summary>Opens (and so migrates) the database.</summary>
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await using var connection = await _connections.OpenAsync(cancellationToken);
    }

    public Task<SessionStatus> GetStatusAsync(CancellationToken cancellationToken) => _signIn.GetStatusAsync(cancellationToken);

    public Task<SignInResult> SignInWithBrowserAsync(CancellationToken cancellationToken) =>
        Announce(_signIn.SignInWithBrowserAsync(_browser, cancellationToken));

    public Task<SignInResult> SignInWithPastedTokenAsync(string token, CancellationToken cancellationToken) =>
        Announce(_signIn.SignInWithPastedTokenAsync(token, cancellationToken));

    public Task<SecretResult> SignOutAsync(CancellationToken cancellationToken) => _signIn.SignOutAsync(cancellationToken);

    public void Dispose() => _gateway.Dispose();

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
