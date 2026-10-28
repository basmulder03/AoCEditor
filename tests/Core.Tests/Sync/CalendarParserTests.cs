using AoCEditor.Core.Sync.Domain;
using Shared.Contracts;
using Shouldly;
using Xunit;

namespace AoCEditor.Core.Tests.Sync;

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

    [Fact]
    public void StarMarksInsideEveryDayDoNotCountAsStars()
    {
        const string html = "<pre class=\"calendar\">"
            + "<a aria-label=\"Day 1, two stars\" href=\"https://adventofcode.com/2020/day/1\" class=\"calendar-day1 calendar-verycomplete\"><span class=\"calendar-mark-complete\">*</span><span class=\"calendar-mark-verycomplete\">*</span></a>\n"
            + "<a aria-label=\"Day 2\" href=\"https://adventofcode.com/2020/day/2\" class=\"calendar-day2\"><span class=\"calendar-mark-complete\">*</span><span class=\"calendar-mark-verycomplete\">*</span></a>\n"
            + "<a aria-label=\"Day 17\" href=\"https://adventofcode.com/2020/day/17\" class=\"calendar-day17\">x</a></pre>";

        CalendarParser.ParseCalendar(html, 2020).ShouldBe([new SyncedDay(1, 2), new SyncedDay(2, 0), new SyncedDay(17, 0)]);
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
