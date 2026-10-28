using Microsoft.Data.Sqlite;
using Shouldly;
using Storage.Infrastructure;
using Xunit;

namespace Storage.Tests;

public class MigrationTests
{
    private static async Task<long> Scalar(SqliteConnection c, string sql)
    {
        await using var cmd = c.CreateCommand();
        cmd.CommandText = sql;
        return Convert.ToInt64(await cmd.ExecuteScalarAsync(TestContext.Current.CancellationToken), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static async Task Exec(SqliteConnection c, string sql)
    {
        await using var cmd = c.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task OpeningTwiceAppliesTheEmbeddedMigrationsOnceAndConfiguresTheConnection()
    {
        using var temp = new TempDir();
        var factory = new SqliteConnectionFactory(new AppDataPaths(temp.Path));
        var ct = TestContext.Current.CancellationToken;

        await using (await factory.OpenAsync(ct)) { }
        await using var conn = await factory.OpenAsync(ct);

        (await Scalar(conn, "SELECT COUNT(*) FROM schema_migrations")).ShouldBe(1);
        (await Scalar(conn, "SELECT COUNT(*) FROM sqlite_master WHERE name IN ('sync_state','kv')")).ShouldBe(2);
        (await Scalar(conn, "PRAGMA foreign_keys")).ShouldBe(1);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "PRAGMA journal_mode";
        ((string?)await cmd.ExecuteScalarAsync(ct)).ShouldBe("wal");
    }

    [Fact]
    public async Task AFailingMigrationIsRolledBackAndEarlierOnesStay()
    {
        using var temp = new TempDir();
        var ct = TestContext.Current.CancellationToken;
        await using var conn = new SqliteConnection($"Data Source={Path.Combine(temp.Path, "t.db")};Pooling=False");
        Directory.CreateDirectory(temp.Path);
        await conn.OpenAsync(ct);

        var ok = new Migration(1, "CREATE TABLE a(x INTEGER);");
        var bad = new Migration(2, "CREATE TABLE b(x INTEGER); INSERT INTO nope VALUES (1);");

        await Should.ThrowAsync<SqliteException>(() => Migrator.ApplyAsync(conn, [ok, bad], ct));

        (await Scalar(conn, "SELECT COUNT(*) FROM schema_migrations")).ShouldBe(1);
        (await Scalar(conn, "SELECT COUNT(*) FROM sqlite_master WHERE name='b'")).ShouldBe(0);
        await Migrator.ApplyAsync(conn, [ok], ct);
        (await Scalar(conn, "SELECT COUNT(*) FROM schema_migrations")).ShouldBe(1);
    }

    [Fact]
    public void UnixMillisecondsRoundTrip()
    {
        var at = new DateTimeOffset(2026, 10, 28, 17, 3, 39, 824, TimeSpan.Zero);

        UnixMs.To(UnixMs.From(at)).ShouldBe(at);
    }
}

public class MigrationLoggingTests
{
    private sealed class Capture : Microsoft.Extensions.Logging.ILogger
    {
        public List<string> Lines { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;

        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Lines.Add($"{logLevel} {formatter(state, exception)}");
    }

    [Fact]
    public async Task AppliedMigrationsAreLogged()
    {
        using var temp = new TempDir();
        var log = new Capture();

        await using var conn = await new SqliteConnectionFactory(new AppDataPaths(temp.Path), log).OpenAsync(TestContext.Current.CancellationToken);

        log.Lines.ShouldContain("Information Applying migration 1");
        log.Lines.ShouldContain("Information Migration 1 applied");
    }

    [Fact]
    public async Task FailingMigrationIsLoggedWithItsReason()
    {
        using var temp = new TempDir();
        var log = new Capture();
        await using var conn = new SqliteConnection("Data Source=:memory:");
        await conn.OpenAsync(TestContext.Current.CancellationToken);

        await Should.ThrowAsync<SqliteException>(() => Migrator.ApplyAsync(conn, [new Migration(1, "NOT SQL")], TestContext.Current.CancellationToken, log));

        log.Lines.ShouldContain(l => l.StartsWith("Error Migration 1 failed", StringComparison.Ordinal));
    }
}
