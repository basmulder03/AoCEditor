namespace Shared.Contracts;

/// <summary>Small persisted settings; never holds secrets.</summary>
public interface IKeyValueStore
{
    Task<string?> GetAsync(string key, CancellationToken cancellationToken);

    Task SetAsync(string key, string value, CancellationToken cancellationToken);
}
