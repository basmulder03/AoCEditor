using Reqnroll;
using Shared;
using Xunit;

namespace Specs;

[Binding]
public sealed class VersionSteps
{
    private string _version = "";

    [When("the app version is requested")]
    public void WhenRequested() => _version = AppInfo.Version;

    [Then("the version is a valid semantic version")]
    public void ThenValid() => Assert.True(System.Version.TryParse(_version, out _));
}
