using Reqnroll;
using Shared;
using Shouldly;

namespace Specs.Steps;

[Binding]
public sealed class VersionSteps
{
    private string _version = "";

    [When("I ask for the app version")]
    public void WhenIAskForTheAppVersion() => _version = AppInfo.Version;

    [Then("I get a valid version number")]
    public void ThenIGetAValidVersionNumber() => System.Version.TryParse(_version, out _).ShouldBeTrue();
}
