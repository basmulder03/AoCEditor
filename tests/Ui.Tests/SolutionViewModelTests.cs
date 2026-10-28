using AoCEditor;
using AoCEditor.Core.Plugins.Domain;
using AoCEditor.Core.Runner.Application;
using Shouldly;
using Xunit;

namespace AoCEditor.UI.Tests;

public class SolutionViewModelTests
{
    private static readonly PluginManifest Python = Plugin("python", "Python");
    private static readonly PluginManifest Dotnet = Plugin("dotnet", ".NET");

    private readonly List<bool> _allowed = [];
    private readonly List<string> _asked = [];
    private DayRunResult _next = new(DayRunKind.Ran, new RunResult(RunStatus.Completed, 0, TimeSpan.FromSeconds(1)));
    private bool _consent = true;
    private readonly SolutionViewModel _model;

    public SolutionViewModelTests() =>
        _model = new SolutionViewModel([Python, Dotnet], RunAsync, detail => { _asked.Add(detail); return Task.FromResult(_consent); }, a => a());

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static PluginManifest Plugin(string id, string name) => ManifestParser.Parse($$"""
        { "schemaVersion": 1, "id": "{{id}}", "name": "{{name}}", "extensions": [".x"], "templateFiles": { "a.x": "x" },
          "toolchain": { "executables": ["t"] }, "run": { "command": ["{source}"], "input": "stdin" } }
        """).Manifest!;

    private Task<DayRunResult> RunAsync(PluginManifest plugin, bool allowUnsandboxed, IProgress<OutputLine> output, CancellationToken ct)
    {
        _allowed.Add(allowUnsandboxed);
        output.Report(new OutputLine(OutputChannel.Stdout, "hi"));
        return Task.FromResult(_next);
    }

    [Fact]
    public void TheFirstPluginIsSelectedAndTheNamesAreListed()
    {
        _model.PluginNames.ShouldBe(["Python", ".NET"]);
        _model.SelectedPlugin.ShouldBe(Python);
    }

    [Fact]
    public async Task ARunShowsItsOutputAndResultInTheConsole()
    {
        await _model.RunAsync(Ct);

        _model.Console.Lines.Select(l => l.Text).ShouldBe(["hi"]);
        _model.Console.Status.ShouldBe("Finished with exit code 0 in 1.00 s");
        _model.IsRunning.ShouldBeFalse();
        _allowed.ShouldBe([false]);
    }

    [Theory]
    [InlineData(DayRunKind.NoWorkspace)]
    [InlineData(DayRunKind.NoToolchain)]
    [InlineData(DayRunKind.InputUnavailable)]
    [InlineData(DayRunKind.CompileFailed)]
    public async Task ProblemsBeforeTheRunAreExplainedInTheConsoleStatus(DayRunKind kind)
    {
        _next = new DayRunResult(kind, null, "Because reasons.");

        await _model.RunAsync(Ct);

        _model.Console.Status.ShouldBe("Because reasons.");
    }

    [Fact]
    public async Task WithoutASandboxTheUserIsAskedAndAYesRunsAgainUnsandboxed()
    {
        _next = new DayRunResult(DayRunKind.SandboxRefused, null, "no sandbox");

        await _model.RunAsync(Ct);

        _allowed.ShouldBe([false, true]);
    }

    [Fact]
    public async Task TheUserIsToldWhichProtectionsAreMissing()
    {
        _next = new DayRunResult(DayRunKind.SandboxRefused, null, "Missing: network isolation.");

        await _model.RunAsync(Ct);

        _asked.ShouldBe(["Missing: network isolation."]);
    }

    [Fact]
    public async Task ANoToTheSandboxQuestionDoesNotRunAnything()
    {
        _consent = false;
        _next = new DayRunResult(DayRunKind.SandboxRefused, null, "no sandbox");

        await _model.RunAsync(Ct);

        _allowed.ShouldBe([false]);
        _model.Console.Status.ShouldContain("not run");
    }

    [Fact]
    public async Task OnlyOneRunAtATime()
    {
        var gate = new TaskCompletionSource<DayRunResult>();
        var model = new SolutionViewModel([Python], (_, _, _, _) => gate.Task, _ => Task.FromResult(true), a => a());

        var first = model.RunAsync(Ct);
        model.CanRun.ShouldBeFalse();
        await model.RunAsync(Ct);
        gate.SetResult(_next);
        await first;

        model.CanRun.ShouldBeTrue();
    }

    [Fact]
    public async Task StoppingCancelsTheRunningProgram()
    {
        var model = new SolutionViewModel([Python], async (_, _, _, ct) =>
        {
            await Task.Delay(Timeout.Infinite, ct);
            return _next;
        }, _ => Task.FromResult(true), a => a());

        var run = model.RunAsync(Ct);
        model.Stop();

        await run;

        model.IsRunning.ShouldBeFalse();
        model.Console.Status.ShouldBe("Cancelled.");
    }

    [Fact]
    public async Task NoLinesAreSentToTheUiOnceTheConsoleIsFull()
    {
        var posts = 0;
        var model = new SolutionViewModel([Python], (_, _, output, _) =>
        {
            for (var i = 0; i < 50_000; i++)
            {
                output.Report(new OutputLine(OutputChannel.Stdout, "x"));
            }

            return Task.FromResult(_next);
        }, _ => Task.FromResult(true), a => { posts++; a(); });

        await model.RunAsync(Ct);

        model.Console.IsTruncated.ShouldBeTrue();
        posts.ShouldBeLessThanOrEqualTo(model.Console.MaxLines + 1);
    }
}
