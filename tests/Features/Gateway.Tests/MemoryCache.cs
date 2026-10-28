using System.Net;
using Gateway;
using Shared.Contracts;

namespace Gateway.Tests;

internal sealed class MemoryCache : IResponseCache
{
    public Dictionary<string, string> Items { get; } = [];

    public ValueTask<string?> TryGetAsync(string path, CancellationToken cancellationToken) =>
        ValueTask.FromResult(Items.TryGetValue(path, out var v) ? v : null);

    public ValueTask SetAsync(string path, string content, CancellationToken cancellationToken)
    {
        Items[path] = content;
        return ValueTask.CompletedTask;
    }
}
