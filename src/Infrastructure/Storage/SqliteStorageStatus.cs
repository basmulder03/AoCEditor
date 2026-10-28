using System.Globalization;
using Microsoft.Data.Sqlite;
using Shared.Contracts;

namespace AoCEditor.Infrastructure.Storage;

public sealed class SqliteStorageStatus(IConnectionFactory connections, IAppDataPaths paths) : IStorageStatus
{
    public async Task<StorageStatus> GetAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = await connections.OpenAsync(cancellationToken);
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*), COALESCE(MAX(version), 0) FROM schema_migrations";
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            await reader.ReadAsync(cancellationToken);
            return new StorageStatus(paths.Database, reader.GetInt32(1), reader.GetInt32(0));
        }
        catch (SqliteException ex)
        {
            return new StorageStatus(paths.Database, 0, 0, ex.Message);
        }
    }
}
