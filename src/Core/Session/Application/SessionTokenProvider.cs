using Shared.Contracts;

namespace Session.Application;

public sealed class SessionTokenProvider(ISecretStore store) : ISessionTokenProvider
{
    internal const string SecretName = "session-token";

    public async ValueTask<string?> GetAsync(CancellationToken cancellationToken) =>
        (await ReadAsync(cancellationToken).ConfigureAwait(false) as SecretResult.Ok)?.Value;

    /// <summary>The store's own answer, so callers can tell a missing token from an unavailable store.</summary>
    public Task<SecretResult> ReadAsync(CancellationToken cancellationToken) => store.GetAsync(SecretName, cancellationToken);

    public async Task<SecretResult> SaveAsync(string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("The session token is empty.", nameof(token));
        }

        return await store.SetAsync(SecretName, token.Trim(), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Signing out when no token is stored counts as success.</summary>
    public async Task<SecretResult> ClearAsync(CancellationToken cancellationToken)
    {
        var result = await store.DeleteAsync(SecretName, cancellationToken).ConfigureAwait(false);
        return result is SecretResult.NotFound ? new SecretResult.Ok() : result;
    }
}
