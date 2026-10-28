using Microsoft.Extensions.Logging.Abstractions;
using AoCEditor.Infrastructure.Storage;
using Shared.Contracts;
using Shouldly;
using Xunit;

namespace AoCEditor.Infrastructure.Tests.Storage;

public class SyncStateStoreTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static SqliteSyncStateStore Create(TempDir temp) => new(new SqliteConnectionFactory(new AppDataPaths(temp.Path), NullLogger<SqliteConnectionFactory>.Instance));

    [Fact]
    public async Task ReplacingAYearKeepsOnlyTheNewDaysAndRemembersWhenItWasSynced()
    {
        using var temp = new TempDir();
        var store = Create(temp);
        var first = new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var second = first.AddDays(1);

        await store.ReplaceYearAsync(2024, [new(1, 2), new(2, 1), new(3, 0)], first, Ct);
        await store.ReplaceYearAsync(2024, [new(1, 2), new(2, 2)], second, Ct);

        (await store.ReadYearAsync(2024, Ct)).ShouldBe([new SyncedDay(1, 2), new SyncedDay(2, 2)]);
        (await store.LastSyncedAsync(2024, Ct)).ShouldBe(second);
    }

    [Fact]
    public async Task UnsyncedYearsHaveNoDaysAndNoSyncTime()
    {
        using var temp = new TempDir();
        var store = Create(temp);

        (await store.ReadYearAsync(2020, Ct)).ShouldBeEmpty();
        (await store.LastSyncedAsync(2020, Ct)).ShouldBeNull();
        (await store.SyncedYearsAsync(Ct)).ShouldBeEmpty();
    }

    [Fact]
    public async Task SyncedYearsListsEachYearOnceNewestFirst()
    {
        using var temp = new TempDir();
        var store = Create(temp);
        var at = DateTimeOffset.UnixEpoch;

        await store.ReplaceYearAsync(2022, [new(1, 1), new(2, 1)], at, Ct);
        await store.ReplaceYearAsync(2024, [new(1, 0)], at, Ct);

        (await store.SyncedYearsAsync(Ct)).ShouldBe([2024, 2022]);
    }

    [Fact]
    public async Task ReplacingWithNoDaysClearsTheYear()
    {
        using var temp = new TempDir();
        var store = Create(temp);

        await store.ReplaceYearAsync(2024, [new(1, 1)], DateTimeOffset.UnixEpoch, Ct);
        await store.ReplaceYearAsync(2024, [], DateTimeOffset.UnixEpoch, Ct);

        (await store.SyncedYearsAsync(Ct)).ShouldBeEmpty();
    }
}
