using AoCEditor.Core.Workspace.Application;
using Reqnroll;
using Shouldly;
using Specs.Drivers.Workspace;

namespace Specs.Steps.Workspace;

[Binding]
public sealed class ScaffoldingSteps(ScaffoldingDriver driver)
{
    [Given("I have chosen a solutions folder")]
    public Task GivenIHaveChosenASolutionsFolder() => driver.ChooseSolutionsFolderAsync();

    [Given("I started day {int} of {int} in {string} and edited the file")]
    public async Task GivenIStartedAndEdited(int day, int year, string language)
    {
        await driver.StartDayAsync(year, day, language);
        driver.EditSolution(year, day, language);
    }

    [When("I start day {int} of {int} in {string}")]
    public Task WhenIStartDay(int day, int year, string language) => driver.StartDayAsync(year, day, language);

    [When("I choose my app data folder as the solutions folder")]
    public Task WhenIChooseAppData() => driver.ChooseAppDataAsSolutionsFolderAsync();

    [Then("the solution folder for day {int} of {int} in {string} holds {int} new file(s)")]
    public void ThenHolds(int day, int year, string language, int count)
    {
        driver.Created.Count.ShouldBe(count);
        File.Exists(driver.SolutionFile(year, day, language)).ShouldBeTrue();
    }

    [Then("my edits are still there")]
    public void ThenEditsRemain() => driver.ReadSolution(2024, 3, "python").ShouldBe("my work\n");

    [Then("the solutions folder is refused")]
    public void ThenRefused() => driver.RootResult.ShouldBe(SetRootResult.OverlapsAppData);
}
