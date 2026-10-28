namespace Shared.Contracts;

/// <summary>Reports the state of the local database for the debug view.</summary>
public interface IStorageStatus
{
    Task<StorageStatus> GetAsync(CancellationToken cancellationToken);
}
