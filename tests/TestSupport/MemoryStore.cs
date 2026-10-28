using Shared.Contracts;

namespace AoCEditor.TestSupport;

/// <summary>In-memory key-value settings and per-year sync state in one fake.</summary>
public sealed class MemoryStore : IKeyValueStore, ISyncStateStore
{
    public Dictionary<string, string> Items { get; } = [];

    public Dictionary<int, List<SyncedDay>> Years { get; } = [];

    public Dictionary<int, DateTimeOffset> SyncedAt { get; } = [];

    public Task<string?> GetAsync(string key, CancellationToken cancellationToken) =>
        Task.FromResult(Items.TryGetValue(key, out var v) ? v : null);

    public Task SetAsync(string key, string value, CancellationToken cancellationToken)
    {
        Items[key] = value;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<SyncedDay>> ReadYearAsync(int year, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<SyncedDay>>(Years.TryGetValue(year, out var d) ? d : []);

    public Task ReplaceYearAsync(int year, IReadOnlyList<SyncedDay> days, DateTimeOffset syncedAt, CancellationToken cancellationToken)
    {
        Years[year] = [.. days];
        SyncedAt[year] = syncedAt;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<int>> SyncedYearsAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<int>>([.. Years.Keys]);

    public Task<DateTimeOffset?> LastSyncedAsync(int year, CancellationToken cancellationToken) =>
        Task.FromResult<DateTimeOffset?>(SyncedAt.TryGetValue(year, out var at) ? at : null);
}
