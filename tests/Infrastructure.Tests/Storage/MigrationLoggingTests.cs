using AoCEditor.Infrastructure.Storage;
using Microsoft.Data.Sqlite;
using Shouldly;
using Xunit;

namespace AoCEditor.Infrastructure.Tests.Storage;

public class MigrationLoggingTests
{
    private sealed class Capture : Microsoft.Extensions.Logging.ILogger<SqliteConnectionFactory>
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

        await Should.ThrowAsync<SqliteException>(() => Migrator.ApplyAsync(conn, [new Migration(1, "NOT SQL")], log, TestContext.Current.CancellationToken));

        log.Lines.ShouldContain(l => l.StartsWith("Error Migration 1 failed", StringComparison.Ordinal));
    }
}
