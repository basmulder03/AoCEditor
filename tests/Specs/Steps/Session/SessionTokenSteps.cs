using Reqnroll;
using Session.Application;
using Shouldly;
using Specs.Drivers.Session;

namespace Specs.Steps.Session;

[Binding]
public sealed class SessionTokenSteps(SessionTokenDriver driver)
{
    [Given("the system has no secret service")]
    public void GivenNoSecretService() => driver.BreakSecretService();

    [Given("I have saved the session token {string}")]
    [When("I save the session token {string}")]
    [When("I try to save the session token {string}")]
    public Task SaveToken(string token) => driver.SaveAsync(token);

    [When("I sign out")]
    public Task WhenISignOut() => driver.SignOutAsync();

    [Then("the app can read the session token {string}")]
    public async Task ThenTokenReadable(string token) => (await driver.ReadAsync()).ShouldBe(token);

    [Then("the app has no session token")]
    public async Task ThenNoToken() => (await driver.ReadAsync()).ShouldBeNull();

    [Then("I am told the secret store is unavailable")]
    public void ThenUnavailable() => driver.Result.ShouldBeOfType<SecretResult.Unavailable>();
}
