using Microsoft.Extensions.Logging.Abstractions;
using AoCEditor.Infrastructure.Storage;
using Shouldly;
using Xunit;

namespace AoCEditor.Infrastructure.Tests.Storage;

public class KeyValueStoreTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ValuesRoundTripAndOverwrite()
    {
        using var temp = new TempDir();
        var store = new SqliteKeyValueStore(new SqliteConnectionFactory(new AppDataPaths(temp.Path), NullLogger<SqliteConnectionFactory>.Instance));

        (await store.GetAsync("a", Ct)).ShouldBeNull();
        await store.SetAsync("a", "1", Ct);
        await store.SetAsync("a", "2", Ct);

        (await store.GetAsync("a", Ct)).ShouldBe("2");
    }

    [Fact]
    public async Task StatusReportsTheAppliedMigrations()
    {
        using var temp = new TempDir();
        var paths = new AppDataPaths(temp.Path);

        var status = await new SqliteStorageStatus(new SqliteConnectionFactory(paths, NullLogger<SqliteConnectionFactory>.Instance), paths).GetAsync(Ct);

        status.SchemaVersion.ShouldBe(2);
        status.MigrationsApplied.ShouldBe(2);
        status.Error.ShouldBeNull();
        status.DatabasePath.ShouldBe(paths.Database);
    }

    [Fact]
    public async Task StatusExplainsAFailureInsteadOfThrowing()
    {
        using var temp = new TempDir();
        var paths = new AppDataPaths(temp.Path);
        await File.WriteAllTextAsync(paths.Database, "this is not a database, just text padded to be long enough to look wrong" + new string('x', 300), Ct);

        var status = await new SqliteStorageStatus(new SqliteConnectionFactory(paths, NullLogger<SqliteConnectionFactory>.Instance), paths).GetAsync(Ct);

        status.Error.ShouldNotBeNullOrEmpty();
    }
}
