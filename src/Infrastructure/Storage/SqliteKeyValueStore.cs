using Shared.Contracts;

namespace AoCEditor.Infrastructure.Storage;

public sealed class SqliteKeyValueStore(IConnectionFactory connections) : IKeyValueStore
{
    public async Task<string?> GetAsync(string key, CancellationToken cancellationToken)
    {
        await using var connection = await connections.OpenAsync(cancellationToken);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT value FROM kv WHERE key = $k";
        cmd.Parameters.AddWithValue("$k", key);
        return await cmd.ExecuteScalarAsync(cancellationToken) as string;
    }

    public async Task SetAsync(string key, string value, CancellationToken cancellationToken)
    {
        await using var connection = await connections.OpenAsync(cancellationToken);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "INSERT INTO kv (key, value) VALUES ($k, $v) ON CONFLICT(key) DO UPDATE SET value = excluded.value";
        cmd.Parameters.AddWithValue("$k", key);
        cmd.Parameters.AddWithValue("$v", value);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }
}
