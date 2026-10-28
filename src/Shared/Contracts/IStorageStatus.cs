namespace Shared.Contracts;

public sealed record StorageStatus(string DatabasePath, int SchemaVersion, int MigrationsApplied, string? Error = null);

/// <summary>Reports the state of the local database for the debug view.</summary>
public interface IStorageStatus
{
    Task<StorageStatus> GetAsync(CancellationToken cancellationToken);
}
