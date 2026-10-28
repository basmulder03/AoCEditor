using Shared.Contracts;
using Shouldly;
using Sync.Domain;
using Xunit;

namespace Sync.Tests;

public class CalendarParserTests
{
    private static readonly DateTimeOffset Oct2026 = new(2026, 10, 28, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Dec2026 = new(2026, 12, 3, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void StarsComeFromTheDayLinkClasses()
    {
        var html = Html.Calendar(2024, (1, 2), (2, 1), (3, 0));

        CalendarParser.ParseCalendar(html, 2024).ShouldBe([new SyncedDay(1, 2), new SyncedDay(2, 1), new SyncedDay(3, 0)]);
    }

    [Fact]
    public void TheNumberOfDaysIsWhateverTheCalendarShows()
    {
        var html = Html.Calendar(2025, Enumerable.Range(1, 12).Select(d => (d, 0)).ToArray());

        CalendarParser.ParseCalendar(html, 2025)!.Count.ShouldBe(12);
    }

    [Fact]
    public void ClassOrderAndAttributeOrderDoNotMatter()
    {
        const string html = "<a class=\"calendar-verycomplete calendar-day4\" aria-label=\"Day 4\" href=\"/2024/day/4\">x</a>";

        CalendarParser.ParseCalendar(html, 2024).ShouldBe([new SyncedDay(4, 2)]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("<html>login page</html>")]
    [InlineData("<a href=\"/2024/day/1\" class=\"calendar-day1\">x</a><a href=\"/2024/day/99\" class=\"calendar-day99\">x</a>")]
    [InlineData("<a href=\"/2024/day/1\" class=\"calendar-day1\">x</a><a href=\"/2024/day/1\" class=\"calendar-day1\">x</a>")]
    public void UnknownMarkupIsAParseFailureNotACrash(string html)
    {
        CalendarParser.ParseCalendar(html, 2024).ShouldBeNull();
    }

    [Fact]
    public void LinksToOtherYearsAreIgnored()
    {
        var html = Html.Calendar(2023, (1, 1)) + Html.Calendar(2024, (2, 2));

        CalendarParser.ParseCalendar(html, 2024).ShouldBe([new SyncedDay(2, 2)]);
    }

    [Fact]
    public void EventYearsAreReadFromTheEventList()
    {
        var html = Html.Events("/2024", "/2023", "/2015");

        CalendarParser.ParseEvents(html, Oct2026).ShouldBe([2024, 2023, 2015]);
    }

    [Fact]
    public void TheLatestEventIsListedAsTheHomePageAndTakesItsYearFromTheClock()
    {
        var html = Html.Events("/", "/2024");

        CalendarParser.ParseEvents(html, Oct2026).ShouldBe([2025, 2024]);
        CalendarParser.ParseEvents(html, Dec2026).ShouldBe([2026, 2024]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("<html>nothing here</html>")]
    public void AnEventPageWithoutYearsIsAParseFailure(string html)
    {
        CalendarParser.ParseEvents(html, Oct2026).ShouldBeNull();
    }
}
