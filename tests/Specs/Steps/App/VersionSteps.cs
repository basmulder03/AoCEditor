using Reqnroll;
using Shouldly;
using Specs.Drivers.App;

namespace Specs.Steps.App;

[Binding]
public sealed class VersionSteps(VersionDriver driver)
{
    [When("I ask for the app version")]
    public void WhenIAskForTheAppVersion() => driver.AskForVersion();

    [Then("I get a valid version number")]
    public void ThenIGetAValidVersionNumber() => System.Version.TryParse(driver.Version, out _).ShouldBeTrue();
}
