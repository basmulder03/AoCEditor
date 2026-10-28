namespace Shared.Contracts;

/// <summary>The single door to the AoC site; every request is caused by a user action.</summary>
public interface IAocGateway
{
    Task<GatewayResult> GetAsync(string path, RequestKind kind, FetchPolicy policy, CancellationToken cancellationToken);

    /// <summary>Submissions are always <see cref="RequestKind.Interactive"/>.</summary>
    Task<GatewayResult> PostFormAsync(string path, IReadOnlyDictionary<string, string> form, CancellationToken cancellationToken);

    /// <summary>Earliest moment a request of this kind may be sent; a time in the past or now means it may go at once.</summary>
    DateTimeOffset NextAllowedAt(RequestKind kind);
}
