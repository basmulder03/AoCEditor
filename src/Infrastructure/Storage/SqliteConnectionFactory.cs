using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Contracts;

namespace Storage.Infrastructure;

public sealed class SqliteConnectionFactory(IAppDataPaths paths, ILogger? logger = null) : IConnectionFactory
{
    private readonly ILogger _log = logger ?? NullLogger.Instance;

    public async Task<SqliteConnection> OpenAsync(CancellationToken ct)
    {
        var connection = new SqliteConnection($"Data Source={paths.Database}");
        try
        {
            await connection.OpenAsync(ct);
            await using var pragma = connection.CreateCommand();
            pragma.CommandText = "PRAGMA journal_mode=WAL; PRAGMA foreign_keys=ON;";
            await pragma.ExecuteNonQueryAsync(ct);
            await Migrator.ApplyAsync(connection, Migrator.Embedded(), ct, _log);
            return connection;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _log.OpenFailed(ex.Message);
            await connection.DisposeAsync();
            throw;
        }
    }
}
