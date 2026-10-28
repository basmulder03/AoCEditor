using Shared.Contracts;

namespace Session.Application;

public sealed class SessionTokenProvider(ISecretStore store) : ISessionTokenProvider
{
    internal const string SecretName = "session-token";

    public async ValueTask<string?> GetAsync(CancellationToken cancellationToken)
    {
        var result = await store.GetAsync(SecretName, cancellationToken).ConfigureAwait(false);
        return (result as SecretResult.Ok)?.Value;
    }

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
