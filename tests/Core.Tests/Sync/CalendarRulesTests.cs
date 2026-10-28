using AoCEditor.Core.Sync.Domain;
using Shouldly;
using Xunit;

namespace AoCEditor.Core.Tests.Sync;

public class CalendarRulesTests
{
    private static readonly TimeSpan Est = TimeSpan.FromHours(-5);

    [Fact]
    public void ADayStaysLockedUntilMidnightEasternStandardTime()
    {
        var justBefore = new DateTimeOffset(2026, 12, 5, 0, 0, 0, Est).AddSeconds(-1);
        var exactly = new DateTimeOffset(2026, 12, 5, 0, 0, 0, Est);

        CalendarRules.StateOf(2026, 5, 0, justBefore).ShouldBe(DayState.Locked);
        CalendarRules.StateOf(2026, 5, 0, exactly).ShouldBe(DayState.Open);
    }

    [Fact]
    public void UnlockMomentIsComparedInUtcNotLocalTime()
    {
        var utcFiveAm = new DateTimeOffset(2026, 12, 1, 5, 0, 0, TimeSpan.Zero);

        CalendarRules.StateOf(2026, 1, 0, utcFiveAm).ShouldBe(DayState.Open);
        CalendarRules.StateOf(2026, 1, 0, utcFiveAm.AddSeconds(-1)).ShouldBe(DayState.Locked);
    }

    [Theory]
    [InlineData(0, DayState.Open)]
    [InlineData(1, DayState.Part1Done)]
    [InlineData(2, DayState.Complete)]
    public void StarsDecideTheStateOfAnUnlockedDay(int stars, DayState expected)
    {
        var now = new DateTimeOffset(2026, 12, 20, 12, 0, 0, TimeSpan.Zero);

        CalendarRules.StateOf(2026, 3, stars, now).ShouldBe(expected);
    }

    [Fact]
    public void EveryDayOfAPastYearIsUnlocked()
    {
        var now = new DateTimeOffset(2026, 10, 28, 0, 0, 0, TimeSpan.Zero);

        CalendarRules.StateOf(2024, 25, 0, now).ShouldBe(DayState.Open);
    }

    [Fact]
    public void ADayOfAFutureYearIsLocked()
    {
        var now = new DateTimeOffset(2026, 10, 28, 0, 0, 0, TimeSpan.Zero);

        CalendarRules.StateOf(2027, 1, 0, now).ShouldBe(DayState.Locked);
    }

    [Fact]
    public void StarsWinOverTheClock()
    {
        var early = new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero);

        CalendarRules.StateOf(2026, 9, 2, early).ShouldBe(DayState.Complete);
    }
}
