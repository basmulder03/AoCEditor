using System.Runtime.Versioning;
using Reqnroll;
using Shouldly;
using Specs.Drivers.Runner;

namespace Specs.Steps.Runner;

[Binding]
public sealed class SandboxSteps(SandboxDriver driver)
{
    [Given("a sandbox is available on this computer")]
    public static void GivenASandboxIsAvailable()
    {
        if (!SandboxDriver.Available)
        {
            // xunit v3 turns this prefix into a skipped test.
            throw new InvalidOperationException("$XunitDynamicSkip$The sandbox needs Linux with Landlock and seccomp.");
        }
    }

    [Given("a private file outside the solution folder")]
    [SupportedOSPlatform("linux")]
    public void GivenAPrivateFile() => driver.CreatePrivateFile();

    [Given("a server is listening on this computer")]
    public void GivenAServer() => driver.StartServer();

    [When("I run a solution that reads the private file")]
    public Task WhenReadsPrivate() => driver.RunShellAsync($"cat '{driver.Private}/secret.txt'");

    [When("I run a solution that writes next to itself")]
    public Task WhenWritesNextToItself() => driver.RunShellAsync("echo x > created.txt");

    [When("I run a solution that makes the private file readable for everyone")]
    public Task WhenChmods() => driver.RunShellAsync($"chmod 777 '{driver.Private}/secret.txt'");

    [When("I run a solution that connects to the server")]
    public Task WhenConnects() => driver.RunConnectAsync();

    [When("I run a solution that uses the processor forever")]
    public Task WhenBusy() => driver.RunBusyLoopAsync();

    [Then("the solution did not get the contents of the private file")]
    public void ThenNoContents() => driver.Output.ShouldNotContain("private-contents");

    [Then("the private file is still private")]
    [SupportedOSPlatform("linux")]
    public void ThenStillPrivate() => driver.PrivateFileMode.ShouldBe(UnixFileMode.UserRead | UnixFileMode.UserWrite);

    [Then("no file was created")]
    public void ThenNoFile() => driver.FilesInSolutionFolder.ShouldBe(0);

    [Then("the server saw no connection")]
    public void ThenNoConnection() => driver.ServerSawConnection.ShouldBeFalse();

    [Then("the run ended within {int} seconds")]
    public void ThenEnded(int seconds) => driver.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(seconds));
}
