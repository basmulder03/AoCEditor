using AoCEditor.Core.Session.Application;
using Reqnroll;
using Shouldly;
using Specs.Drivers.Session;

namespace Specs.Steps.Session;

[Binding]
public sealed class SignInSteps(SignInDriver driver)
{
    [Given("Advent of Code accepts my login")]
    public void GivenAccepts() => driver.SiteAcceptsLogin();

    [Given("Advent of Code rejects my login")]
    [Given("I am no longer signed in")]
    public void GivenRejects() => driver.SiteRejectsLogin();

    [Given("the embedded browser is not available")]
    public void GivenNoBrowser() => driver.BrowserIsMissing();

    [Given("this machine has no secure storage")]
    public void GivenNoSecretService() => driver.BreakSecretService();

    [Given("I have signed in with the browser")]
    [When("I sign in with the browser")]
    public Task SignInWithBrowser() => driver.SignInWithBrowserAsync();

    [Given("the browser still remembers an old login that has expired")]
    public void GivenStaleLogin() => driver.BrowserFindsRememberedLoginThenMine();

    [Given("I do not finish signing in")]
    public void GivenNeverFinish() => driver.BrowserStaysOpen();

    [When("{int} minutes pass with the sign-in window open")]
    public Task MinutesPass(int minutes) => driver.LeaveBrowserOpenForMinutesAsync(minutes);

    [When("I close the sign-in browser")]
    public async Task CloseBrowser()
    {
        driver.BrowserWindowIsClosed();
        await driver.SignInWithBrowserAsync();
    }

    [When("I sign in by pasting the token {string}")]
    public Task SignInByPasting(string token) => driver.SignInWithTokenAsync(token);

    [When("I sign out of Advent of Code")]
    public Task SignOut() => driver.SignOutAsync();

    [When("Advent of Code refuses three requests in a row")]
    public Task Refuse() => driver.RefuseRequestsAsync(3);

    [Then("I am signed in")]
    public void ThenSignedIn() => driver.Result.ShouldBeOfType<SignInResult.SignedIn>();

    [Then("the sign-in is cancelled")]
    public void ThenCancelled() => driver.Result.ShouldBeOfType<SignInResult.Cancelled>();

    [Then("the sign-in timed out")]
    public void ThenTimedOut() => driver.Result.ShouldBeOfType<SignInResult.TimedOut>();

    [Given("Advent of Code cannot be reached")]
    public void GivenDown() => driver.SiteIsDown();

    [Then("I am told Advent of Code could not be reached")]
    public void ThenUnreachable() => driver.Result.ShouldBeOfType<SignInResult.NetworkError>();

    [Then("I am told the token is not valid")]
    public void ThenInvalid() => driver.Result.ShouldBeOfType<SignInResult.InvalidToken>();

    [Then("I am told secure storage is unavailable")]
    public void ThenUnavailable() => driver.Result.ShouldBeOfType<SignInResult.StoreUnavailable>();

    [Then("I am offered to paste the token instead")]
    public void ThenPasteOffered() => driver.Result.ShouldBeOfType<SignInResult.BrowserUnsupported>();

    [Then("Advent of Code was not asked about the token")]
    public void ThenNotAsked() => driver.TokenChecks.ShouldBe(0);

    [Then("a token is stored")]
    public async Task ThenHasToken() => (await driver.HasTokenAsync()).ShouldBeTrue();

    [Then("no token is stored")]
    public async Task ThenNoToken() => (await driver.HasTokenAsync()).ShouldBeFalse();

    [Then("the browser remembers nothing about me")]
    public void ThenProfileCleared() => driver.ProfileCleared.ShouldBeTrue();

    [Then("I am asked to sign in once")]
    public void ThenAskedOnce() => driver.Prompts.ShouldBe(1);
}
