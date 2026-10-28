using Runner.Application;
using Runner.UI;
using Shouldly;
using Xunit;

namespace Runner.Tests;

public class ConsoleViewModelTests
{
    [Fact]
    public void LinesKeepTheirOrderAndStream()
    {
        var console = new ConsoleViewModel();

        console.Append(new OutputLine(OutputChannel.Stdout, "a"));
        console.Append(new OutputLine(OutputChannel.Stderr, "b"));

        console.Lines.Select(l => (l.Text, l.IsError)).ShouldBe([("a", false), ("b", true)]);
    }

    [Fact]
    public void ARunawayProgramCannotGrowTheConsoleWithoutLimit()
    {
        var console = new ConsoleViewModel(maxLines: 3);

        for (var i = 1; i <= 5; i++)
        {
            console.Append(new OutputLine(OutputChannel.Stdout, i.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        }

        console.Lines.Select(l => l.Text).ShouldBe(["1", "2", "3"]);
        console.IsTruncated.ShouldBeTrue();
    }

    [Fact]
    public void StartingARunClearsTheOldOutput()
    {
        var console = new ConsoleViewModel();
        console.Append(new OutputLine(OutputChannel.Stdout, "old"));

        console.Begin();

        console.Lines.ShouldBeEmpty();
        console.IsRunning.ShouldBeTrue();
        console.Status.ShouldBe("Running...");
    }

    [Theory]
    [InlineData(RunStatus.Completed, 0, "Finished with exit code 0 in 1.50 s")]
    [InlineData(RunStatus.Completed, 3, "Failed with exit code 3 in 1.50 s")]
    [InlineData(RunStatus.TimedOut, null, "Stopped after the time limit (1.50 s)")]
    [InlineData(RunStatus.Cancelled, null, "Cancelled after 1.50 s")]
    [InlineData(RunStatus.FailedToStart, null, "Could not start: boom")]
    [InlineData(RunStatus.SandboxUnavailable, null, "Not started: boom")]
    public void TheEndOfARunIsSummarisedInWords(RunStatus status, int? exit, string expected)
    {
        var console = new ConsoleViewModel();
        console.Begin();

        console.End(new RunResult(status, exit, TimeSpan.FromMilliseconds(1500), "boom"));

        console.Status.ShouldBe(expected);
        console.IsRunning.ShouldBeFalse();
    }
}
