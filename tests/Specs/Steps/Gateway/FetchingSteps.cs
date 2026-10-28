using Gateway;
using Reqnroll;
using Shouldly;
using Specs.Drivers.Gateway;

namespace Specs.Steps.Gateway;

[Binding]
public sealed class FetchingSteps(FetchingDriver driver)
{
    [Given("I am signed in")]
    public static void GivenIAmSignedIn()
    {
    }

    [Given("I have already downloaded a page")]
    public Task GivenIHaveAlreadyDownloadedAPage() => driver.SavePageAsync("saved copy");

    [Given("Advent of Code reports that the page does not exist")]
    public void GivenNotFound() => driver.ReportPageMissing();

    [Given("I have just made a request")]
    public Task GivenIHaveJustMadeARequest() => driver.RequestFirstAsync();

    [When("I open that page")]
    public Task WhenIOpenThatPage() => driver.OpenPageAsync(FetchPolicy.CacheFirst);

    [When("I explicitly refresh that page")]
    public Task WhenIRefresh() => driver.OpenPageAsync(FetchPolicy.Refresh);

    [When("I make another request straight away")]
    public Task WhenIMakeAnotherRequest() => driver.StartSecondRequestAsync();

    [Then("I see the saved copy")]
    public void ThenSavedCopy() => driver.Result.ShouldBe(new GatewayResult.Ok("saved copy"));

    [Then("Advent of Code was not contacted")]
    public void ThenNotContacted() => driver.Requests.ShouldBeEmpty();

    [Then("Advent of Code was contacted once")]
    public void ThenContactedOnce() => driver.Requests.Count.ShouldBe(1);

    [Then("it is held back")]
    public void ThenHeldBack() => driver.Requests.ShouldBe(["/first"]);

    [Then("it goes ahead after the minimum spacing has passed")]
    public async Task ThenGoesAhead()
    {
        await driver.WaitMinimumSpacingAsync();
        driver.Requests.ShouldBe(["/first", "/second"]);
    }

    [Then("I am told it was not found")]
    public void ThenNotFound() => driver.Result.ShouldBeOfType<GatewayResult.NotFound>();

    [Then("nothing is saved for it")]
    public async Task ThenNothingSaved() => (await driver.SavedPageAsync()).ShouldBeNull();
}
