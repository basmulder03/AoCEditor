using AoCEditor.Core.Session.Application;


namespace Specs.Drivers.Session;

public sealed class SessionTokenDriver
{
    private readonly MemoryStore _store = new();
    private readonly SessionTokenProvider _provider;

    public SessionTokenDriver() => _provider = new SessionTokenProvider(_store);

    public SecretResult? Result { get; private set; }

    public void BreakSecretService() => _store.Down = true;

    public async Task SaveAsync(string token) => Result = await _provider.SaveAsync(token, CancellationToken.None);

    public Task SignOutAsync() => _provider.ClearAsync(CancellationToken.None);

    public async Task<string?> ReadAsync() => await _provider.GetAsync(CancellationToken.None);

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
