using Reqnroll;
using Shouldly;
using Specs.Drivers.Diagnostics;

namespace Specs.Steps.Diagnostics;

[Binding]
public sealed class DiagnosticsSteps(DiagnosticsDriver driver)
{
    [Given("debug mode is off")]
    public void GivenDebugModeIsOff() => driver.StartWithDebugOff();

    [Given("debug mode is on")]
    [Given("the app is started with the debug flag")]
    public void GivenDebugModeIsOn() => driver.StartWithDebugOn();

    [When("I switch debug mode on")]
    public Task WhenISwitchDebugModeOn() => driver.SwitchDebugOnAsync();

    [When("the app is restarted")]
    public Task WhenTheAppIsRestarted() => driver.RestartAsync();

    [When("the app records a detailed message")]
    public void WhenTheAppRecordsADetailedMessage() => driver.RecordDetailedMessage();

    [When("the app records a message containing my session cookie")]
    public void WhenTheAppRecordsACookie() => driver.RecordCookie();

    [When("the app records a message naming a file in my home folder")]
    public void WhenTheAppRecordsAHomeFile() => driver.RecordHomeFile();

    [When("I ask for the diagnostics")]
    public Task WhenIAskForTheDiagnostics() => driver.AskForDiagnosticsAsync();

    [Then("debug mode is on")]
    public void ThenDebugModeIsOn() => driver.DebugIsOn.ShouldBeTrue();

    [Then("the detailed message is kept")]
    public void ThenTheDetailedMessageIsKept() => driver.Messages.ShouldContain("detailed step");

    [Then("the detailed message is not kept")]
    public void ThenTheDetailedMessageIsNotKept() => driver.Messages.ShouldNotContain("detailed step");

    [Then("the recorded message hides the cookie")]
    public void ThenTheRecordedMessageHidesTheCookie()
    {
        driver.Messages.ShouldNotBeEmpty();
        driver.Messages.ShouldAllBe(m => !m.Contains(DiagnosticsDriver.Cookie, StringComparison.Ordinal));
    }

    [Then("the log file hides the cookie")]
    public void ThenTheLogFileHidesTheCookie() => driver.LogFile.ShouldNotContain(DiagnosticsDriver.Cookie);

    [Then("the recorded message shows the file under a tilde")]
    public void ThenTheFileIsUnderATilde() => driver.Messages.ShouldContain("opened ~/notes/day1.txt");

    [Then("the diagnostics hide the cookie")]
    public void ThenTheDiagnosticsHideTheCookie() => driver.Report.ShouldNotBeNull().ShouldNotContain(DiagnosticsDriver.Cookie);
}
