using Diagnostics.Application;
using Shared.Contracts;

namespace Diagnostics.Tests;

internal sealed class MemoryStore : IKeyValueStore
{
    public Dictionary<string, string> Items { get; } = [];

    public Task<string?> GetAsync(string key, CancellationToken cancellationToken) =>
        Task.FromResult(Items.TryGetValue(key, out var v) ? v : null);

    public Task SetAsync(string key, string value, CancellationToken cancellationToken)
    {
        Items[key] = value;
        return Task.CompletedTask;
    }
}
