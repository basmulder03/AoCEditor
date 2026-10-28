namespace Shared.Contracts;

/// <summary>Stars earned on one day as last seen on the calendar page.</summary>
public sealed record SyncedDay(int Day, int Stars);

/// <summary>Persisted result of calendar syncs.</summary>
public interface ISyncStateStore
{
    Task<IReadOnlyList<SyncedDay>> ReadYearAsync(int year, CancellationToken cancellationToken);

    /// <summary>Replaces all days of the year in one transaction.</summary>
    Task ReplaceYearAsync(int year, IReadOnlyList<SyncedDay> days, DateTimeOffset syncedAt, CancellationToken cancellationToken);

    Task<IReadOnlyList<int>> SyncedYearsAsync(CancellationToken cancellationToken);

    Task<DateTimeOffset?> LastSyncedAsync(int year, CancellationToken cancellationToken);
}
