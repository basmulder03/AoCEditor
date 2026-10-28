using Reqnroll;
using Shouldly;
using Storage.Domain;

namespace Specs.Steps;

[Binding]
public sealed class DataLocationSteps
{
    private string _solutions = "";
    private bool _allowed;

    [Given("my solutions folder is {string}")]
    public void GivenMySolutionsFolderIs(string folder) => _solutions = Path.GetFullPath(folder, Path.GetTempPath());

    [When("I choose {string} as the app data location")]
    public void WhenIChooseAsTheAppDataLocation(string folder) =>
        _allowed = DataRootValidator.IsAllowed(Path.GetFullPath(folder, Path.GetTempPath()), _solutions);

    [Then("the app data location is refused")]
    public void ThenRefused() => _allowed.ShouldBeFalse();

    [Then("the app data location is accepted")]
    public void ThenAccepted() => _allowed.ShouldBeTrue();
}
