using Reqnroll;
using Shouldly;
using Specs.Drivers.Calendar;

namespace Specs.Steps.Calendar;

[Binding]
public sealed class CalendarSteps(CalendarDriver driver)
{
    [Given("the date is {int} December {int}")]
    public void GivenDecember(int day, int year) => driver.SetToday(year, 12, day);

    [Given("the date is {int} October {int}")]
    public void GivenOctober(int day, int year) => driver.SetToday(year, 10, day);

    [Given("my calendar for {int} has two stars on day {int} and one star on day {int}")]
    public void GivenStars(int year, int complete, int half) => driver.Store(year, (complete, 2), (half, 1));

    [Given("my calendar for {int} has one star on day {int}")]
    public void GivenOneStar(int year, int day) => driver.Store(year, (day, 1));

    [Given("Advent of Code allows the next request in {int} seconds")]
    public void GivenWait(int seconds) => driver.SetWait(seconds);

    [Given("Advent of Code has a calendar for {int} with two stars on day {int}")]
    public void GivenRemote(int year, int day) => driver.Remote(year, (day, 2));

    [When("I open the calendar")]
    public Task WhenOpen() => driver.OpenAsync();

    [When("I switch to {int}")]
    public Task WhenSwitch(int year) => driver.SwitchAsync(year);

    [When("I sync this year")]
    public Task WhenSync() => driver.SyncYearAsync();

    [Then("day {int} shows {string}")]
    public void ThenDay(int day, string label) => driver.LabelOf(day).ShouldBe(label);

    [Then("the calendar says {string}")]
    public void ThenSays(string text) => driver.Model!.LastSyncedText.ShouldBe(text);

    [Then("the progress mentioned {int} and {int} seconds")]
    public void ThenProgress(int year, int seconds)
    {
        driver.ProgressTexts.ShouldContain(t => t.Contains($"Syncing {year},", StringComparison.Ordinal) && t.Contains($"{seconds} s"));
    }

    [Then("the sync result says {string}")]
    public void ThenResult(string text) => driver.Model!.ResultText.ShouldBe(text);
}
