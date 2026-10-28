using System.Net;
using Gateway;
using Shared.Contracts;

namespace Gateway.Tests;

internal sealed class FakeTokens(string? token) : ISessionTokenProvider
{
    public ValueTask<string?> GetAsync(CancellationToken cancellationToken) => ValueTask.FromResult(token);
}
