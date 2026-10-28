namespace Shared.Contracts;

/// <summary>Supplies the AoC session token, or null when the user is not signed in.</summary>
public interface ISessionTokenProvider
{
    ValueTask<string?> GetAsync(CancellationToken cancellationToken);
}
