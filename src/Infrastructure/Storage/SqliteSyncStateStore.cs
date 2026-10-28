using Microsoft.Data.Sqlite;
using Shared.Contracts;

namespace AoCEditor.Infrastructure.Storage;

public sealed class SqliteSyncStateStore(IConnectionFactory connections) : ISyncStateStore
{
    public async Task<IReadOnlyList<SyncedDay>> ReadYearAsync(int year, CancellationToken cancellationToken)
    {
        await using var connection = await connections.OpenAsync(cancellationToken);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT day, stars FROM sync_state WHERE year = $y ORDER BY day";
        cmd.Parameters.AddWithValue("$y", year);
        var days = new List<SyncedDay>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            days.Add(new SyncedDay(reader.GetInt32(0), reader.GetInt32(1)));
        }

        return days;
    }

    public async Task ReplaceYearAsync(int year, IReadOnlyList<SyncedDay> days, DateTimeOffset syncedAt, CancellationToken cancellationToken)
    {
        await using var connection = await connections.OpenAsync(cancellationToken);
        await using var tx = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
        await using (var delete = connection.CreateCommand())
        {
            delete.Transaction = tx;
            delete.CommandText = "DELETE FROM sync_state WHERE year = $y";
            delete.Parameters.AddWithValue("$y", year);
            await delete.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (var day in days)
        {
            await using var insert = connection.CreateCommand();
            insert.Transaction = tx;
            insert.CommandText = "INSERT INTO sync_state (year, day, stars, synced_at) VALUES ($y, $d, $s, $at)";
            insert.Parameters.AddWithValue("$y", year);
            insert.Parameters.AddWithValue("$d", day.Day);
            insert.Parameters.AddWithValue("$s", day.Stars);
            insert.Parameters.AddWithValue("$at", UnixMilliseconds.From(syncedAt));
            await insert.ExecuteNonQueryAsync(cancellationToken);
        }

        await tx.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<int>> SyncedYearsAsync(CancellationToken cancellationToken)
    {
        await using var connection = await connections.OpenAsync(cancellationToken);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT DISTINCT year FROM sync_state ORDER BY year DESC";
        var years = new List<int>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            years.Add(reader.GetInt32(0));
        }

        return years;
    }

    public async Task<DateTimeOffset?> LastSyncedAsync(int year, CancellationToken cancellationToken)
    {
        await using var connection = await connections.OpenAsync(cancellationToken);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT MAX(synced_at) FROM sync_state WHERE year = $y";
        cmd.Parameters.AddWithValue("$y", year);
        return await cmd.ExecuteScalarAsync(cancellationToken) is long ms ? UnixMilliseconds.To(ms) : null;
    }
}
