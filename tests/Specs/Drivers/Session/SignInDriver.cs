using System.Net;
using Gateway;
using Microsoft.Extensions.Time.Testing;
using Session.Application;
using Shared.Contracts;

namespace Specs.Drivers.Session;

public sealed class SignInDriver : IDisposable
{
    private const string BrowserToken = "synthetic-browser-token";
    private readonly Store _store = new();
    private readonly Handler _handler = new();
    private readonly FakeTimeProvider _time = new();
    private readonly SessionEvents _events = new();
    private readonly SessionTokenProvider _tokens;
    private readonly AocGateway _gateway;
    private readonly SignInCoordinator _coordinator;
    private BrowserOutcome _outcome = new BrowserOutcome.Token(BrowserToken);
    private int _prompts;

    public SignInDriver()
    {
        _tokens = new SessionTokenProvider(_store);
        _gateway = new AocGateway(_handler, _tokens, new NoCache(), new GatewayOptions { InteractiveInterval = TimeSpan.FromSeconds(1) }, _time, _events);
        _coordinator = new SignInCoordinator(_tokens, _gateway, () => ProfileCleared = true);
        _events.SignInRequired += (_, _) => _prompts++;
    }

    public SignInResult? Result { get; private set; }

    public bool ProfileCleared { get; private set; }

    public int Prompts => _prompts;

    public int TokenChecks => _handler.Requests;

    public void SiteAcceptsLogin() => _handler.Status = HttpStatusCode.OK;

    public void SiteRejectsLogin() => _handler.Status = HttpStatusCode.Found;

    public void BreakSecretService() => _store.Down = true;

    public void BrowserWindowIsClosed() => _outcome = new BrowserOutcome.Cancelled();

    public void BrowserIsMissing() => _outcome = new BrowserOutcome.Unsupported("missing");

    public async Task SignInWithBrowserAsync() =>
        Result = await _coordinator.SignInWithBrowserAsync(new Browser(_outcome), CancellationToken.None);

    public async Task SignInWithTokenAsync(string token) =>
        Result = await _coordinator.SignInWithPastedTokenAsync(token, CancellationToken.None);

    public Task SignOutAsync() => _coordinator.SignOutAsync(CancellationToken.None);

    public async Task<bool> HasTokenAsync() => await _tokens.GetAsync(CancellationToken.None) is not null;

    public async Task RefuseRequestsAsync(int count)
    {
        for (var i = 0; i < count; i++)
        {
            await _gateway.GetAsync("/any", RequestKind.Interactive, FetchPolicy.Refresh, CancellationToken.None);
            _time.Advance(TimeSpan.FromSeconds(1));
        }
    }

    public void Dispose()
    {
        _gateway.Dispose();
        _handler.Dispose();
    }

    private sealed class Browser(BrowserOutcome outcome) : ISignInBrowser
    {
        public Task<BrowserOutcome> AcquireTokenAsync(CancellationToken cancellationToken) => Task.FromResult(outcome);
    }

    private sealed class Handler : HttpMessageHandler
    {
        public HttpStatusCode Status { get; set; } = HttpStatusCode.Found;

        public int Requests { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests++;
            return Task.FromResult(new HttpResponseMessage(Status) { Content = new StringContent("page") });
        }
    }

    private sealed class NoCache : IResponseCache
    {
        public ValueTask<string?> TryGetAsync(string path, CancellationToken cancellationToken) => ValueTask.FromResult<string?>(null);

        public ValueTask SetAsync(string path, string content, CancellationToken cancellationToken) => ValueTask.CompletedTask;
    }

    private sealed class Store : ISecretStore
    {
        private readonly Dictionary<string, string> _items = [];

        public bool Down { get; set; }

        public Task<SecretResult> GetAsync(string name, CancellationToken cancellationToken) =>
            Task.FromResult<SecretResult>(Down ? new SecretResult.Unavailable("down")
                : _items.TryGetValue(name, out var v) ? new SecretResult.Ok(v) : new SecretResult.NotFound());

        public Task<SecretResult> SetAsync(string name, string value, CancellationToken cancellationToken)
        {
            if (Down)
            {
                return Task.FromResult<SecretResult>(new SecretResult.Unavailable("down"));
            }

            _items[name] = value;
            return Task.FromResult<SecretResult>(new SecretResult.Ok());
        }

        public Task<SecretResult> DeleteAsync(string name, CancellationToken cancellationToken) =>
            Task.FromResult<SecretResult>(Down ? new SecretResult.Unavailable("down")
                : _items.Remove(name) ? new SecretResult.Ok() : new SecretResult.NotFound());
    }
}
