using System.Globalization;
using System.Reflection;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace AoCEditor.Infrastructure.Storage;

public static class Migrator
{
    public static async Task ApplyAsync(SqliteConnection connection, IReadOnlyList<Migration> migrations, ILogger log, CancellationToken cancellationToken)
    {
        await RunAsync(connection, null, "CREATE TABLE IF NOT EXISTS schema_migrations (version INTEGER NOT NULL PRIMARY KEY, applied_at INTEGER NOT NULL)", cancellationToken);
        var applied = new HashSet<int>();
        await using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT version FROM schema_migrations";
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                applied.Add(reader.GetInt32(0));
            }
        }

        var pending = migrations.OrderBy(m => m.Version).Where(m => !applied.Contains(m.Version)).ToList();
        log.MigrationState(applied.Count, pending.Count);
        foreach (var m in pending)
        {
            log.MigrationApplying(m.Version);
            await using var tx = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
            try
            {
                await RunAsync(connection, tx, m.Sql, cancellationToken);
            }
            catch (SqliteException ex)
            {
                log.MigrationFailed(m.Version, ex.Message);
                throw;
            }

            await using var record = connection.CreateCommand();
            record.Transaction = tx;
            record.CommandText = "INSERT INTO schema_migrations (version, applied_at) VALUES ($v, $at)";
            record.Parameters.AddWithValue("$v", m.Version);
            record.Parameters.AddWithValue("$at", UnixMs.From(DateTimeOffset.UtcNow));
            await record.ExecuteNonQueryAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
            log.MigrationApplied(m.Version);
        }
    }

    public static IReadOnlyList<Migration> Embedded()
    {
        var assembly = typeof(Migrator).Assembly;
        return [.. assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith("Migrations/", StringComparison.Ordinal) && n.EndsWith(".sql", StringComparison.Ordinal))
            .Select(n => new Migration(
                int.Parse(Path.GetFileName(n).Split('_')[0], CultureInfo.InvariantCulture),
                Read(assembly, n)))];
    }

    private static string Read(Assembly assembly, string name)
    {
        using var reader = new StreamReader(assembly.GetManifestResourceStream(name)!);
        return reader.ReadToEnd();
    }

    private static async Task RunAsync(SqliteConnection connection, SqliteTransaction? tx, string sql, CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }
}
