namespace Shared.Contracts;

/// <summary>The single door to the AoC site; every request is caused by a user action.</summary>
public interface IAocGateway
{
    Task<GatewayResult> GetAsync(string path, FetchPolicy policy, CancellationToken cancellationToken);

    Task<GatewayResult> PostFormAsync(string path, IReadOnlyDictionary<string, string> form, CancellationToken cancellationToken);
}
