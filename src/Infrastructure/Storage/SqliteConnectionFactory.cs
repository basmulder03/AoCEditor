using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Shared.Contracts;

namespace AoCEditor.Infrastructure.Storage;

public sealed class SqliteConnectionFactory(IAppDataPaths paths, ILogger<SqliteConnectionFactory> log) : IConnectionFactory
{

    public async Task<SqliteConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection($"Data Source={paths.Database}");
        try
        {
            await connection.OpenAsync(cancellationToken);
            await using var pragma = connection.CreateCommand();
            pragma.CommandText = "PRAGMA journal_mode=WAL; PRAGMA foreign_keys=ON;";
            await pragma.ExecuteNonQueryAsync(cancellationToken);
            await Migrator.ApplyAsync(connection, Migrator.Embedded(), log, cancellationToken);
            return connection;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            log.OpenFailed(ex.Message);
            await connection.DisposeAsync();
            throw;
        }
    }
}
