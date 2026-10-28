using Reqnroll;
using Shouldly;
using Specs.Drivers.Sync;
using Sync.Application;
using Sync.Domain;

namespace Specs.Steps.Sync;

[Binding]
public sealed class CalendarSyncSteps(CalendarSyncDriver driver)
{
    [Given("I am not signed in")]
    public void GivenNotSignedIn() => driver.SignOut();

    [Given("the event ran in {int} and {int}")]
    public void GivenEventRanInTwoYears(int first, int second) => driver.RunEvents(first, second);

    [Given("the event ran in {int}")]
    public void GivenEventRanIn(int year) => driver.RunEvents(year);

    [Given("today is {int} October {int}")]
    public void GivenToday(int day, int year) => driver.SetToday(year, 10, day);

    [Given("I have two stars on day {int} and one star on day {int} of {int}")]
    public void GivenStars(int complete, int half, int year) => driver.SetStars(year, (complete, 2), (half, 1));

    [Given("I have no stars in {int}")]
    public void GivenNoStars(int year) => driver.SetStars(year);

    [Given("Advent of Code shows an unexpected page for {int}")]
    public void GivenUnexpectedPage(int year) => driver.ShowUnexpectedPage(year);

    [When("I sync my calendar")]
    public Task WhenISync() => driver.SyncAsync();

    [Then("{int} year(s) was/were synced")]
    public void ThenYearsSynced(int count) => driver.Summary!.YearsSynced.Count.ShouldBe(count);

    [Then("day {int} of {int} is complete")]
    public void ThenComplete(int day, int year) => driver.StateOf(year, day).ShouldBe(DayState.Complete);

    [Then("day {int} of {int} is half done")]
    public void ThenHalfDone(int day, int year) => driver.StateOf(year, day).ShouldBe(DayState.Part1Done);

    [Then("day {int} of {int} is open")]
    public void ThenOpen(int day, int year) => driver.StateOf(year, day).ShouldBe(DayState.Open);

    [Then("day {int} of {int} is locked")]
    public void ThenLocked(int day, int year) => driver.StateOf(year, day).ShouldBe(DayState.Locked);

    [Then("the event list was requested once")]
    public void ThenEventListOnce() => driver.Requests.Count(r => r == "/events").ShouldBe(1);

    [Then("each year was requested once")]
    public void ThenEachYearOnce() => driver.Requests.Where(r => r != "/events").ShouldBe(["/2024", "/2023"], ignoreOrder: true);

    [Then("{int} is reported as unreadable")]
    public void ThenUnreadable(int year) => driver.Summary!.Errors.ShouldContain(new YearError(year, SyncError.ParseError));

    [Then("I am told to sign in")]
    public void ThenSignIn() => driver.Summary!.Outcome.ShouldBe(SyncOutcome.NotSignedIn);
}
