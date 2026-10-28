using Microsoft.Data.Sqlite;
using Shared.Contracts;

namespace Storage.Infrastructure;

public interface IConnectionFactory
{
    /// <summary>Opens a migrated connection with WAL and foreign keys on.</summary>
    Task<SqliteConnection> OpenAsync(CancellationToken ct);
}

public sealed class SqliteConnectionFactory(IAppDataPaths paths) : IConnectionFactory
{
    public async Task<SqliteConnection> OpenAsync(CancellationToken ct)
    {
        var connection = new SqliteConnection($"Data Source={paths.Database}");
        try
        {
            await connection.OpenAsync(ct);
            await using var pragma = connection.CreateCommand();
            pragma.CommandText = "PRAGMA journal_mode=WAL; PRAGMA foreign_keys=ON;";
            await pragma.ExecuteNonQueryAsync(ct);
            await Migrator.ApplyAsync(connection, Migrator.Embedded(), ct);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }
}
