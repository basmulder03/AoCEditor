using AoCEditor.Core.Session.Application;
using Shouldly;
using Xunit;

namespace AoCEditor.Core.Tests.Session;

internal sealed class FakeSecretStore : ISecretStore
{
    private readonly Dictionary<string, string> _items = [];

    public bool Unavailable { get; init; }

    public Task<SecretResult> GetAsync(string name, CancellationToken cancellationToken) =>
        Task.FromResult<SecretResult>(Unavailable ? Down()
            : _items.TryGetValue(name, out var v) ? new SecretResult.Ok(v) : new SecretResult.NotFound());

    public Task<SecretResult> SetAsync(string name, string value, CancellationToken cancellationToken)
    {
        if (Unavailable)
        {
            return Task.FromResult<SecretResult>(Down());
        }

        _items[name] = value;
        return Task.FromResult<SecretResult>(new SecretResult.Ok());
    }

    public Task<SecretResult> DeleteAsync(string name, CancellationToken cancellationToken) =>
        Task.FromResult<SecretResult>(Unavailable ? Down()
            : _items.Remove(name) ? new SecretResult.Ok() : new SecretResult.NotFound());

    private static SecretResult.Unavailable Down() => new("no service");
}
