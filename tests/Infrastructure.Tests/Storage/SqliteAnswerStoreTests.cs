using Microsoft.Extensions.Logging.Abstractions;
using AoCEditor.Infrastructure.Storage;
using Shared.Contracts;
using Shouldly;
using Xunit;

namespace AoCEditor.Infrastructure.Tests.Storage;

public class SqliteAnswerStoreTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static readonly DateTimeOffset T0 = new(2026, 12, 3, 10, 0, 0, TimeSpan.Zero);

    private static SqliteAnswerStore Create(TempDir temp) => new(new SqliteConnectionFactory(new AppDataPaths(temp.Path), NullLogger<SqliteConnectionFactory>.Instance));

    [Fact]
    public async Task AttemptsComeBackOldestFirstPerPuzzlePart()
    {
        using var temp = new TempDir();
        var store = Create(temp);

        await store.AddAttemptAsync(2020, 3, 1, new("5", "Wrong", T0), Ct);
        await store.AddAttemptAsync(2020, 3, 1, new("6", "TooHigh", T0.AddMinutes(2)), Ct);
        await store.AddAttemptAsync(2020, 3, 2, new("7", "Correct", T0.AddHours(1)), Ct);

        (await store.AttemptsAsync(2020, 3, 1, Ct)).ShouldBe([new("5", "Wrong", T0), new("6", "TooHigh", T0.AddMinutes(2))]);
        (await store.AttemptsAsync(2020, 4, 1, Ct)).ShouldBeEmpty();
    }

    [Fact]
    public async Task ACooldownIsReplacedAndSurvivesANewStoreInstance()
    {
        using var temp = new TempDir();
        await Create(temp).SetCooldownAsync(2020, 3, 1, T0.AddMinutes(1), Ct);
        await Create(temp).SetCooldownAsync(2020, 3, 1, T0.AddMinutes(5), Ct);

        (await Create(temp).CooldownUntilAsync(2020, 3, 1, Ct)).ShouldBe(T0.AddMinutes(5));
        (await Create(temp).CooldownUntilAsync(2020, 3, 2, Ct)).ShouldBeNull();
    }

    [Fact]
    public async Task TheKnownAnswerIsOverwrittenByTheLatest()
    {
        using var temp = new TempDir();
        var store = Create(temp);

        (await store.KnownAnswerAsync(2020, 3, 1, Ct)).ShouldBeNull();
        await store.SetKnownAnswerAsync(2020, 3, 1, "242", Ct);
        await store.SetKnownAnswerAsync(2020, 3, 1, "243", Ct);

        (await store.KnownAnswerAsync(2020, 3, 1, Ct)).ShouldBe("243");
        (await store.KnownAnswerAsync(2020, 3, 2, Ct)).ShouldBeNull();
    }
}
