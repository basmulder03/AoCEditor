using System.Net;
using Shared.Contracts;

namespace AoCEditor.Infrastructure.Tests.Gateway;

internal sealed class FakeTokens(string? token) : ISessionTokenProvider
{
    public ValueTask<string?> GetAsync(CancellationToken cancellationToken) => ValueTask.FromResult(token);
}
