using System.Diagnostics;
using AoCEditor.Core.Runner.Application;
using AoCEditor.Infrastructure.Runner;
using Shouldly;
using Xunit;

namespace AoCEditor.Infrastructure.Tests.Runner;

public sealed class ProcessRunnerTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "aoc-run-" + Guid.NewGuid().ToString("N"));
    private readonly ProcessRunner _runner = new(SandboxLevel.None);
    private readonly List<OutputLine> _lines = [];

    public ProcessRunnerTests() => Directory.CreateDirectory(_dir);

    public static bool RunsHere => !OperatingSystem.IsWindows();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private RunRequest Request(string[] command, TimeSpan? timeout = null, string? input = null) =>
        new(command, _dir, input, timeout ?? TimeSpan.FromSeconds(20), AllowUnsandboxed: true);

    private Task<RunResult> RunAsync(RunRequest request, CancellationToken? ct = null) =>
        _runner.RunAsync(request, new SyncProgress(_lines), ct ?? Ct);

    [Fact]
    public async Task ARunWithoutAnySandboxIsRefusedUnlessTheUserAcceptedIt()
    {
        var result = await _runner.RunAsync(new RunRequest(["sh"], _dir, null, TimeSpan.FromSeconds(5)), new SyncProgress(_lines), Ct);

        result.Status.ShouldBe(RunStatus.SandboxUnavailable);
        _runner.Level.ShouldBe(SandboxLevel.None);
    }

    [Fact(Skip = "Uses sh", SkipUnless = nameof(RunsHere), SkipType = typeof(ProcessRunnerTests))]
    public async Task OutputIsStreamedPerStreamAndTheExitCodeIsReported()
    {
        var result = await RunAsync(Request(["sh", "-c", "echo out1; echo err1 >&2; echo out2; exit 3"]));

        result.Status.ShouldBe(RunStatus.Completed);
        result.ExitCode.ShouldBe(3);
        _lines.Where(l => l.Stream == OutputChannel.Stdout).Select(l => l.Text).ShouldBe(["out1", "out2"]);
        _lines.Where(l => l.Stream == OutputChannel.Stderr).Select(l => l.Text).ShouldBe(["err1"]);
        result.Elapsed.ShouldBeGreaterThan(TimeSpan.Zero);
    }

    [Fact(Skip = "Uses sh", SkipUnless = nameof(RunsHere), SkipType = typeof(ProcessRunnerTests))]
    public async Task TheInputFileIsPipedToStdin()
    {
        var input = Path.Combine(_dir, "input.txt");
        await File.WriteAllTextAsync(input, "line one\nline two\n", Ct);

        await RunAsync(Request(["cat"], input: input));

        _lines.Select(l => l.Text).ShouldBe(["line one", "line two"]);
    }

    [Fact(Skip = "Uses sh", SkipUnless = nameof(RunsHere), SkipType = typeof(ProcessRunnerTests))]
    public async Task ArgumentsReachTheProgramUntouchedBecauseNoShellIsInvolved()
    {
        await RunAsync(Request(["printf", "%s", "a;b $(echo hacked) `x`"]));

        _lines.Single().Text.ShouldBe("a;b $(echo hacked) `x`");
    }

    [Fact(Skip = "Uses sh", SkipUnless = nameof(RunsHere), SkipType = typeof(ProcessRunnerTests))]
    public async Task TheProgramDoesNotInheritTheAppsEnvironment()
    {
        Environment.SetEnvironmentVariable("AOC_TEST_SECRET", "hunter2");
        try
        {
            await RunAsync(Request(["sh", "-c", "echo ${AOC_TEST_SECRET:-unset}"]));
        }
        finally
        {
            Environment.SetEnvironmentVariable("AOC_TEST_SECRET", null);
        }

        _lines.Single().Text.ShouldBe("unset");
    }

    [Fact(Skip = "Uses sh", SkipUnless = nameof(RunsHere), SkipType = typeof(ProcessRunnerTests))]
    public async Task ATimeoutKillsTheWholeProcessTree()
    {
        var watch = Stopwatch.StartNew();

        var result = await RunAsync(Request(["sh", "-c", "sleep 30 & wait"], timeout: TimeSpan.FromMilliseconds(400)));

        result.Status.ShouldBe(RunStatus.TimedOut);
        watch.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(10));
    }

    [Fact(Skip = "Uses sh", SkipUnless = nameof(RunsHere), SkipType = typeof(ProcessRunnerTests))]
    public async Task CancellingStopsTheRun()
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(Ct);
        cts.CancelAfter(TimeSpan.FromMilliseconds(300));

        var result = await RunAsync(Request(["sleep", "30"]), cts.Token);

        result.Status.ShouldBe(RunStatus.Cancelled);
    }

    [Fact]
    public async Task AProgramThatCannotStartIsReportedNotThrown()
    {
        var result = await RunAsync(Request(["definitely-not-a-program-aoc"]));

        result.Status.ShouldBe(RunStatus.FailedToStart);
        result.Error.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task AnEmptyCommandIsRefused()
    {
        await Should.ThrowAsync<ArgumentException>(() => RunAsync(Request([])));
    }

    [Fact(Skip = "Uses sh", SkipUnless = nameof(RunsHere), SkipType = typeof(ProcessRunnerTests))]
    public async Task AHugeLineWithoutNewlineIsTruncatedWithAnEllipsis()
    {
        var result = await RunAsync(Request(["sh", "-c", "head -c 5000000 /dev/zero | tr '\\0' x"]));

        result.Status.ShouldBe(RunStatus.Completed);
        var line = _lines.First(l => l.Text.StartsWith('x'));
        line.Text.Length.ShouldBeLessThanOrEqualTo(8 * 1024 + 1);
        line.Text.ShouldEndWith("\u2026");
        _lines.Sum(l => l.Text.Length).ShouldBeLessThan(64 * 1024);
    }

    [Fact(Skip = "Uses sh", SkipUnless = nameof(RunsHere), SkipType = typeof(ProcessRunnerTests))]
    public async Task EndlessOutputStopsBeingReportedAtTheByteCapWithOneNote()
    {
        var result = await RunAsync(Request(["yes"], timeout: TimeSpan.FromSeconds(3)));

        result.Status.ShouldBe(RunStatus.TimedOut);
        _lines.Sum(l => l.Text.Length).ShouldBeLessThan(5 * 1024 * 1024);
        _lines.Count(l => l.Stream == OutputChannel.Stderr && l.Text.Contains("limit", StringComparison.OrdinalIgnoreCase)).ShouldBe(1);
    }

    internal sealed class SyncProgress(List<OutputLine> sink) : IProgress<OutputLine>
    {
        public void Report(OutputLine value)
        {
            lock (sink)
            {
                sink.Add(value);
            }
        }
    }
}
