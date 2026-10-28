using System.Net;
using System.Net.Sockets;
using Runner.Application;
using Shouldly;
using Xunit;

namespace Runner.Tests;

public sealed class SandboxTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "aoc-sbx-" + Guid.NewGuid().ToString("N"));
    private readonly List<OutputLine> _lines = [];

    public SandboxTests()
    {
        Directory.CreateDirectory(Solution);
        Directory.CreateDirectory(Outside);
        File.WriteAllText(Path.Combine(Outside, "secret.txt"), "canary-secret");
        File.WriteAllText(Path.Combine(Solution, "main.txt"), "solution-text");
        File.WriteAllText(InputFile, "input-text");
    }

    public static bool Enforced { get; } = OperatingSystem.IsLinux() && LinuxSandbox.Detect() == SandboxLevel.Full;

    public static bool EnforcedWithPython => Enforced && File.Exists("/usr/bin/python3");

    private const string NeedsLandlock = "Needs Linux with Landlock (filesystem) and seccomp (network); this kernel does not provide them.";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private string Solution => Path.Combine(_root, "solution");

    private string Outside => Path.Combine(_root, "outside");

    private string InputFile => Path.Combine(_root, "outside", "input.txt");

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private async Task<RunResult> RunAsync(string[] command, SandboxLimits? limits = null, TimeSpan? timeout = null, bool network = false, IReadOnlyList<string>? readable = null, IReadOnlyList<string>? writable = null, string? input = null, ProcessRunner? runner = null, bool allowUnsandboxed = false) =>
        await (runner ?? new ProcessRunner()).RunAsync(
            new RunRequest(command, Solution, input, timeout ?? TimeSpan.FromSeconds(20), allowUnsandboxed, readable, writable, network, limits),
            new ProcessRunnerTests.SyncProgress(_lines),
            Ct);

    private string Text => string.Join("\n", _lines.Select(l => l.Text));

    [Fact(Skip = NeedsLandlock, SkipUnless = nameof(Enforced), SkipType = typeof(SandboxTests))]
    public async Task AFileOutsideTheAllowedFoldersCannotBeRead()
    {
        var result = await RunAsync(["sh", "-c", $"cat '{Outside}/secret.txt'"]);

        result.ExitCode.ShouldNotBe(0);
        Text.ShouldNotContain("canary-secret");
    }

    [Fact(Skip = NeedsLandlock, SkipUnless = nameof(Enforced), SkipType = typeof(SandboxTests))]
    public async Task TheHomeFolderAndItsDotFoldersAreNotReadable()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        var result = await RunAsync(["sh", "-c", $"ls '{home}'"]);

        result.ExitCode.ShouldNotBe(0);
    }

    [Fact(Skip = NeedsLandlock, SkipUnless = nameof(Enforced), SkipType = typeof(SandboxTests))]
    public async Task NothingCanBeWrittenOutsideTheRunTempFolder()
    {
        var result = await RunAsync(["sh", "-c", $"echo x > '{Outside}/new.txt'; echo x > '{Solution}/new.txt'; echo x > /tmp/aoc-sbx-escape-{Path.GetFileName(_root)}"]);

        File.Exists(Path.Combine(Outside, "new.txt")).ShouldBeFalse();
        File.Exists(Path.Combine(Solution, "new.txt")).ShouldBeFalse();
        File.Exists($"/tmp/aoc-sbx-escape-{Path.GetFileName(_root)}").ShouldBeFalse();
        result.ExitCode.ShouldNotBe(0);
    }

    [Fact(Skip = NeedsLandlock, SkipUnless = nameof(Enforced), SkipType = typeof(SandboxTests))]
    public async Task TheRunTempFolderIsWritableIsHomeAndIsRemovedAfterwards()
    {
        await RunAsync(["sh", "-c", "echo ok > \"$TMPDIR/a\" && cat \"$TMPDIR/a\"; echo \"$TMPDIR\"; echo \"$HOME\""]);

        var lines = _lines.Select(l => l.Text).ToArray();
        lines[0].ShouldBe("ok");
        lines[2].ShouldBe(lines[1]);
        Directory.Exists(lines[1]).ShouldBeFalse();
    }

    [Fact(Skip = NeedsLandlock, SkipUnless = nameof(Enforced), SkipType = typeof(SandboxTests))]
    public async Task ExtraWritableFoldersCanBeGrantedForABuild()
    {
        var result = await RunAsync(["sh", "-c", "echo built > out.txt"], writable: [Solution]);

        result.ExitCode.ShouldBe(0);
        File.ReadAllText(Path.Combine(Solution, "out.txt")).ShouldBe("built\n");
    }

    [Fact(Skip = NeedsLandlock, SkipUnless = nameof(Enforced), SkipType = typeof(SandboxTests))]
    public async Task TheSolutionFolderAndTheGrantedInputCanBeRead()
    {
        var result = await RunAsync(["sh", "-c", $"cat main.txt; cat '{InputFile}'"], readable: [InputFile]);

        result.ExitCode.ShouldBe(0);
        Text.ShouldBe("solution-textinput-text");
    }

    [Fact(Skip = NeedsLandlock, SkipUnless = nameof(EnforcedWithPython), SkipType = typeof(SandboxTests))]
    public async Task APythonProgramRunsAndReadsItsInputFromStdin()
    {
        var result = await RunAsync(["python3", "-c", "import sys; print(sum(int(x) for x in sys.stdin.read().split()))"], input: await WriteNumbersAsync());

        result.ExitCode.ShouldBe(0);
        Text.ShouldBe("6");
    }

    private async Task<string> WriteNumbersAsync()
    {
        var path = Path.Combine(Outside, "numbers.txt");
        await File.WriteAllTextAsync(path, "1 2 3", Ct);
        return path;
    }

    [Fact(Skip = NeedsLandlock, SkipUnless = nameof(EnforcedWithPython), SkipType = typeof(SandboxTests))]
    public async Task ALocalTcpServerCannotBeReached()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        await RunAsync(["python3", "-c", $"import socket\ntry:\n socket.create_connection(('127.0.0.1',{port}),timeout=3); print('connected')\nexcept OSError: print('blocked')"]);

        Text.ShouldBe("blocked");
        listener.Pending().ShouldBeFalse();
    }

    [Fact(Skip = NeedsLandlock, SkipUnless = nameof(EnforcedWithPython), SkipType = typeof(SandboxTests))]
    public async Task ABuildStepThatIsGrantedTheNetworkCanConnect()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        await RunAsync(["python3", "-c", $"import socket\ntry:\n socket.create_connection(('127.0.0.1',{port}),timeout=3); print('connected')\nexcept OSError: print('blocked')"], network: true);

        Text.ShouldBe("connected");
    }

    [Fact(Skip = NeedsLandlock, SkipUnless = nameof(EnforcedWithPython), SkipType = typeof(SandboxTests))]
    public async Task AMemoryHogIsStoppedQuickly()
    {
        var result = await RunAsync(["python3", "-c", "x = bytearray(3 * 1024**3); print('allocated')"], new SandboxLimits(512L * 1024 * 1024, 20, 200, 1024 * 1024));

        Text.ShouldContain("MemoryError");
        result.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(10));
    }

    [Fact(Skip = NeedsLandlock, SkipUnless = nameof(Enforced), SkipType = typeof(SandboxTests))]
    public async Task ABusyLoopIsKilledByTheCpuLimit()
    {
        var result = await RunAsync(["sh", "-c", "while :; do :; done"], new SandboxLimits(1L << 30, 1, 200, 1024 * 1024));

        result.Status.ShouldBe(RunStatus.Completed);
        result.ExitCode.ShouldBe(128 + 24);
        result.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(10));
    }

    [Fact(Skip = NeedsLandlock, SkipUnless = nameof(Enforced), SkipType = typeof(SandboxTests))]
    public async Task AFileSizeLimitStopsRunawayOutputFiles()
    {
        var result = await RunAsync(["sh", "-c", "head -c 5000000 /dev/zero > \"$TMPDIR/big\" && echo done"], new SandboxLimits(1L << 30, 20, 200, 1024 * 1024));

        Text.ShouldNotContain("done");
        result.ExitCode.ShouldBe(128 + 25);
    }

    [Fact(Skip = NeedsLandlock, SkipUnless = nameof(EnforcedWithPython), SkipType = typeof(SandboxTests))]
    public async Task TheNumberOfProcessesIsCapped()
    {
        var script = "import subprocess\nn=0\nfor i in range(400):\n try:\n  subprocess.Popen(['sleep','3']); n+=1\n except OSError: break\nprint(n)";

        var result = await RunAsync(["python3", "-c", script], new SandboxLimits(1L << 32, 20, 40, 1024 * 1024));

        int.Parse(Text, System.Globalization.CultureInfo.InvariantCulture).ShouldBeLessThan(100);
        result.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(10));
    }

    [Fact(Skip = NeedsLandlock, SkipUnless = nameof(Enforced), SkipType = typeof(SandboxTests))]
    public async Task AForkBombEndsQuicklyAndLeavesNothingBehind()
    {
        var result = await RunAsync(["sh", "-c", ":(){ :|:& };:; sleep 30"], new SandboxLimits(1L << 30, 20, 60, 1024 * 1024), TimeSpan.FromSeconds(3));

        result.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(15));
        await Task.Delay(500, Ct);
        Directory.GetDirectories("/proc").Count(d => File.Exists(Path.Combine(d, "cmdline")) && File.ReadAllText(Path.Combine(d, "cmdline")).Contains(":(){", StringComparison.Ordinal)).ShouldBe(0);
    }

    [Fact(Skip = NeedsLandlock, SkipUnless = nameof(Enforced), SkipType = typeof(SandboxTests))]
    public async Task ABelowFullSandboxIsRefusedAndSaysWhatIsMissing()
    {
        var runner = new ProcessRunner(SandboxLevel.Limits);

        var result = await RunAsync(["sh", "-c", "echo hi"], runner: runner);

        result.Status.ShouldBe(RunStatus.SandboxUnavailable);
        result.Error.ShouldNotBeNull();
        result.Error.ShouldContain("filesystem isolation");
        result.Error.ShouldContain("network isolation");
    }

    [Fact(Skip = NeedsLandlock, SkipUnless = nameof(Enforced), SkipType = typeof(SandboxTests))]
    public async Task AcceptedLimitsOnlyRunsStillGetTheLimitsButNotTheFilesystemRules()
    {
        var runner = new ProcessRunner(SandboxLevel.Limits);

        var result = await RunAsync(["sh", "-c", $"cat '{Outside}/secret.txt'; ulimit -c"], runner: runner, allowUnsandboxed: true);

        result.ExitCode.ShouldBe(0);
        Text.ShouldBe("canary-secret0");
    }
}
