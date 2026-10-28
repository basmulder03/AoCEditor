using Reqnroll;
using Session.Application;
using Shouldly;

namespace Specs.Steps;

[Binding]
public sealed class SessionTokenSteps
{
    private readonly MemoryStore _store = new();
    private readonly SessionTokenProvider _provider;
    private SecretResult? _result;

    public SessionTokenSteps() => _provider = new SessionTokenProvider(_store);

    [Given("the system has no secret service")]
    public void GivenNoSecretService() => _store.Down = true;

    [Given("I have saved the session token {string}")]
    [When("I save the session token {string}")]
    public async Task SaveToken(string token) => await _provider.SaveAsync(token, CancellationToken.None);

    [When("I try to save the session token {string}")]
    public async Task WhenITrySave(string token) => _result = await _provider.SaveAsync(token, CancellationToken.None);

    [When("I sign out")]
    public async Task WhenISignOut() => await _provider.ClearAsync(CancellationToken.None);

    [Then("the app can read the session token {string}")]
    public async Task ThenTokenReadable(string token) => (await _provider.GetAsync(CancellationToken.None)).ShouldBe(token);

    [Then("the app has no session token")]
    public async Task ThenNoToken() => (await _provider.GetAsync(CancellationToken.None)).ShouldBeNull();

    [Then("I am told the secret store is unavailable")]
    public void ThenUnavailable() => _result.ShouldBeOfType<SecretResult.Unavailable>();

    private sealed class MemoryStore : ISecretStore
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
