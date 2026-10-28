using Reqnroll;
using Shouldly;
using Specs.Drivers.Storage;

namespace Specs.Steps.Storage;

[Binding]
public sealed class DataLocationSteps(DataLocationDriver driver)
{
    [Given("my solutions folder is {string}")]
    public void GivenMySolutionsFolderIs(string folder) => driver.SetSolutionsFolder(folder);

    [When("I choose {string} as the app data location")]
    public void WhenIChooseAsTheAppDataLocation(string folder) => driver.ChooseDataLocation(folder);

    [Then("the app data location is refused")]
    public void ThenRefused() => driver.Allowed.ShouldBeFalse();

    [Then("the app data location is accepted")]
    public void ThenAccepted() => driver.Allowed.ShouldBeTrue();
}
