using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Versioning;
using AoCEditor.Core.Runner.Application;
using AoCEditor.Infrastructure.Runner;
using Shouldly;
using Xunit;

namespace AoCEditor.Infrastructure.Tests.Runner;

[SupportedOSPlatform("linux")]
public sealed class SandboxHardeningTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "aoc-hard-" + Guid.NewGuid().ToString("N"));
    private readonly List<OutputLine> _lines = [];

    public SandboxHardeningTests()
    {
        Directory.CreateDirectory(Solution);
        File.WriteAllText(Path.Combine(Solution, "main.txt"), "solution-text");
    }

    public static bool EnforcedWithPython => SandboxTests.EnforcedWithPython;

    public static bool Enforced => SandboxTests.Enforced;

    public static bool EnforcedWithDotnet => Enforced && OnPath("dotnet");

    public static bool ScopedSignals => EnforcedWithPython && LinuxSandbox.LandlockAbi() >= 6;

    public static bool OnLinux => OperatingSystem.IsLinux();

    private const string Needs = "Needs Linux with Landlock (filesystem) and seccomp (network) and python3; this computer does not provide them.";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private string Solution => Path.Combine(_root, "solution");

    private string Text => string.Join("\n", _lines.Select(l => l.Text));

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private static bool OnPath(string name) =>
        (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(':').Any(d => d.Length > 0 && File.Exists(Path.Combine(d, name)));

    private async Task<RunResult> RunAsync(string[] command, bool network = false, SandboxLimits? limits = null) =>
        await new ProcessRunner().RunAsync(
            new RunRequest(command, Solution, null, TimeSpan.FromSeconds(30), AllowNetwork: network, Limits: limits),
            new ProcessRunnerTests.SyncProgress(_lines),
            Ct);

    private Task<RunResult> PythonAsync(string script, bool network = false) => RunAsync(["python3", "-c", script], network);

    [Theory(Skip = Needs, SkipUnless = nameof(EnforcedWithPython), SkipType = typeof(SandboxHardeningTests))]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LocalUnixSocketsAreUnreachableEvenWhenTheNetworkIsGranted(bool network)
    {
        var socketPath = Path.Combine(_root, "agent.sock");
        using var listener = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        listener.Bind(new UnixDomainSocketEndPoint(socketPath));
        listener.Listen();

        await PythonAsync(
            $"import socket\ntry:\n s=socket.socket(socket.AF_UNIX); s.connect('{socketPath}'); print('connected')\nexcept OSError: print('blocked')",
            network);

        Text.ShouldBe("blocked");
        listener.Poll(0, SelectMode.SelectRead).ShouldBeFalse();
    }

    [Fact(Skip = Needs, SkipUnless = nameof(EnforcedWithPython), SkipType = typeof(SandboxHardeningTests))]
    public async Task ABuildStepCanCreateInternetSocketsButNoOtherFamily()
    {
        await PythonAsync(
            "import socket\nr=[]\nfor n,f,t in (('inet',socket.AF_INET,socket.SOCK_STREAM),('inet6',socket.AF_INET6,socket.SOCK_DGRAM),('netlink',socket.AF_NETLINK,socket.SOCK_RAW),('unix',socket.AF_UNIX,socket.SOCK_STREAM)):\n try: socket.socket(f,t).close(); r.append(n+':ok')\n except OSError as e: r.append(n+':'+('denied' if isinstance(e,PermissionError) else 'other'))\nprint(' '.join(r))",
            network: true);

        Text.ShouldStartWith("inet:ok");
        Text.ShouldEndWith("netlink:denied unix:denied");
    }

    [Fact(Skip = Needs, SkipUnless = nameof(EnforcedWithPython), SkipType = typeof(SandboxHardeningTests))]
    public async Task ProcessInspectionAndKernelInterfacesAreRefused()
    {
        await PythonAsync(
            "import ctypes,os,subprocess\nlibc=ctypes.CDLL(None,use_errno=True)\np=subprocess.Popen(['sleep','5'])\nr=[]\nr.append('ptrace:%d'%(libc.ptrace(16,p.pid,None,None)==0))\np.kill()\ntry: os.pidfd_open(os.getpid()); r.append('pidfd:True')\nexcept PermissionError: r.append('pidfd:False')\nlibc.syscall(425,0,0); r.append('uring:%d'%ctypes.get_errno())\nprint(' '.join(r))");

        Text.ShouldBe("ptrace:0 pidfd:False uring:1");
    }

    [Fact(Skip = Needs, SkipUnless = nameof(EnforcedWithPython), SkipType = typeof(SandboxHardeningTests))]
    public async Task SignalingEveryProcessOfTheUserIsRefusedButTheOwnGroupIsReachable()
    {
        await PythonAsync("import os\nr=[]\nfor n,f in (('all',lambda: os.kill(-1,0)),('self',lambda: os.kill(os.getpid(),0)),('group',lambda: os.killpg(os.getpgrp(),0))):\n try: f(); r.append(n+':ok')\n except PermissionError: r.append(n+':denied')\nprint(' '.join(r))");

        Text.ShouldBe("all:denied self:ok group:ok");
    }

    [Fact(Skip = "Needs Landlock ABI 6 (kernel 6.12) for signal scoping.", SkipUnless = nameof(ScopedSignals), SkipType = typeof(SandboxHardeningTests))]
    public async Task AProcessOutsideTheSandboxCannotBeSignaled()
    {
        using var outsider = Process.Start(new ProcessStartInfo("sleep", "30") { UseShellExecute = false })!;
        try
        {
            await PythonAsync($"import os\ntry: os.kill({outsider.Id},0); print('reached')\nexcept PermissionError: print('denied')");

            Text.ShouldBe("denied");
        }
        finally
        {
            outsider.Kill();
        }
    }

    [Fact(Skip = Needs, SkipUnless = nameof(EnforcedWithPython), SkipType = typeof(SandboxHardeningTests))]
    public async Task FilePermissionsOwnersTimestampsAndAttributesCannotBeChanged()
    {
        var file = Path.Combine(Solution, "main.txt");
        var mode = File.GetUnixFileMode(file);
        var stamp = File.GetLastWriteTimeUtc(file);

        await PythonAsync(
            $"import os\nf='{file}'\nr=[]\nfor n,a in (('chmod',lambda: os.chmod(f,0o777)),('chown',lambda: os.chown(f,-1,-1)),('utime',lambda: os.utime(f,(0,0))),('xattr',lambda: os.setxattr(f,'user.x',b'1'))):\n try: a(); r.append(n+':ok')\n except PermissionError: r.append(n+':denied')\n except OSError: r.append(n+':other')\nprint(' '.join(r))");

        Text.ShouldBe("chmod:denied chown:denied utime:denied xattr:denied");
        File.GetUnixFileMode(file).ShouldBe(mode);
        File.GetLastWriteTimeUtc(file).ShouldBe(stamp);
    }

    [Fact(Skip = Needs, SkipUnless = nameof(EnforcedWithPython), SkipType = typeof(SandboxHardeningTests))]
    public async Task SessionsNamespacesAndNewProcessGroupsAreRefused()
    {
        await PythonAsync(
            "import ctypes,os\nlibc=ctypes.CDLL(None,use_errno=True)\ndef child(f):\n pid=os.fork()\n if pid==0:\n  try: os._exit(0 if f() else 1)\n  except OSError: os._exit(1)\n return 'ok' if os.waitpid(pid,0)[1]==0 else 'denied'\ndef sys(n,*a):\n return lambda: libc.syscall(n,*a)==0\nr=[]\nr.append('setsid:'+child(lambda: os.setsid() or True))\nr.append('setpgid:'+child(lambda: os.setpgid(0,0) or True))\nr.append('unshare:'+child(lambda: libc.unshare(0x10000000)==0))\nlibc.syscall(435,0,0); r.append('clone3:%d'%ctypes.get_errno())\nprint(' '.join(r))");

        Text.ShouldBe("setsid:denied setpgid:denied unshare:denied clone3:38");
    }

    [Fact(Skip = Needs, SkipUnless = nameof(EnforcedWithPython), SkipType = typeof(SandboxHardeningTests))]
    public async Task EverydayProgramsStillWork()
    {
        await RunAsync(["sh", "-c", "sleep 1 & wait; echo shell-ok"]);
        await PythonAsync("import subprocess,threading\nt=threading.Thread(target=lambda: None); t.start(); t.join()\nprint(subprocess.run(['echo','py-ok'],capture_output=True,text=True).stdout.strip())");
        await RunAsync(["sh", "-c", "echo build-ok > \"$TMPDIR/x\" && cat \"$TMPDIR/x\""], network: true);

        Text.ShouldBe("shell-ok\npy-ok\nbuild-ok");
    }

    [Fact(Skip = "Needs a sandbox and dotnet on PATH.", SkipUnless = nameof(EnforcedWithDotnet), SkipType = typeof(SandboxHardeningTests))]
    public async Task DotnetStillStarts()
    {
        var result = await RunAsync(["dotnet", "--version"]);

        result.ExitCode.ShouldBe(0);
        Text.ShouldMatch(@"^\d+\.\d+\.\d+");
    }

    [Fact(Skip = Needs, SkipUnless = nameof(EnforcedWithPython), SkipType = typeof(SandboxHardeningTests))]
    public void ADescriptorLeakedByTheCallerIsClosedBeforeTheProgramRuns()
    {
        var plan = new SandboxPlan([], [], SandboxLimits.Default, Filesystem: false, Network: false, AllowInternet: false, ["sh", "-c", "{ echo x >&9; } 2>/dev/null && echo open || echo closed"]);
        var host = Environment.ProcessPath!;
        var info = new ProcessStartInfo("python3") { RedirectStandardOutput = true, UseShellExecute = false };
        foreach (var arg in new[] { "-c", "import os,sys\nfd=os.open('/dev/null',os.O_RDWR)\nos.dup2(fd,9)\nos.set_inheritable(9,True)\nos.execv(sys.argv[1],sys.argv[1:])", host })
        {
            info.ArgumentList.Add(arg);
        }

        if (Path.GetFileNameWithoutExtension(host) == "dotnet")
        {
            info.ArgumentList.Add(Environment.GetCommandLineArgs()[0]);
        }

        foreach (var arg in plan.ToArguments())
        {
            info.ArgumentList.Add(arg);
        }

        using var process = Process.Start(info)!;
        var output = process.StandardOutput.ReadToEnd().Trim();
        process.WaitForExit();

        output.ShouldBe("closed");
    }

    [Fact(Skip = Needs, SkipUnless = nameof(Enforced), SkipType = typeof(SandboxHardeningTests))]
    public async Task AHelperThatCannotConfineItselfGivesASandboxFailureNotAProgramExit()
    {
        var result = await RunAsync(["sh", "-c", "echo hi"], limits: new SandboxLimits(1L << 30, -1, 10, 1L << 20));

        result.Status.ShouldBe(RunStatus.SandboxFailed);
        result.ExitCode.ShouldBeNull();
        result.Error.ShouldNotBeNull();
        result.Error.ShouldContain("resource limits");
        Text.ShouldNotContain("hi");
    }

    [Fact(Skip = Needs, SkipUnless = nameof(Enforced), SkipType = typeof(SandboxHardeningTests))]
    public async Task AProgramThatExitsWith125IsStillACompletedRun()
    {
        var result = await RunAsync(["sh", "-c", "exit 125"]);

        result.Status.ShouldBe(RunStatus.Completed);
        result.ExitCode.ShouldBe(125);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(2, false)]
    [InlineData(3, true)]
    [InlineData(8, true)]
    public void FilesystemIsolationNeedsTruncateControl(int landlockAbi, bool reported) =>
        LinuxSandbox.LevelFor(landlockAbi, seccomp: true).HasFlag(SandboxLevel.Filesystem).ShouldBe(reported);

    [Fact(Skip = "Needs Linux paths.", SkipUnless = nameof(OnLinux), SkipType = typeof(SandboxHardeningTests))]
    public void PathEntriesAreResolvedBeforeTheHomeCheckAndProtectedFoldersAreNeverGranted()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var appData = Path.Combine(_root, "appdata");
        var toHome = Path.Combine(_root, "innocent");
        var toSsh = Path.Combine(_root, "tools");
        Directory.CreateSymbolicLink(toHome, home);
        Directory.CreateSymbolicLink(toSsh, Path.Combine(home, ".ssh"));
        var path = string.Join(':', toHome, toSsh, Path.Combine(home, ".config", "bin"), Path.Combine(appData, "bin"), "/usr/bin");
        var runner = new ProcessRunner(SandboxLevel.Full, [appData]);

        var plan = runner.BuildPlan(new RunRequest(["sh"], Solution, null, TimeSpan.FromSeconds(1)), null, path);

        plan.Readable.ShouldContain("/usr/bin");
        plan.Readable.ShouldNotContain(toHome);
        plan.Readable.ShouldNotContain(toSsh);
        plan.Readable.ShouldNotContain(home);
        plan.Readable.ShouldNotContain(Path.Combine(home, ".config", "bin"));
        plan.Readable.ShouldNotContain(Path.Combine(appData, "bin"));
    }

    [Fact]
    public void ABuildStepPlanAllowsInternetSocketsOnlyWhenAskedFor()
    {
        var runner = new ProcessRunner(SandboxLevel.Full);
        var request = new RunRequest(["sh"], Solution, null, TimeSpan.FromSeconds(1));

        var run = runner.BuildPlan(request, null, string.Empty);
        var build = runner.BuildPlan(request with { AllowNetwork = true }, null, string.Empty);

        (run.Network, run.AllowInternet).ShouldBe((true, false));
        (build.Network, build.AllowInternet).ShouldBe((true, true));
    }
}
