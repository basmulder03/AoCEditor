using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace Runner.Application;

/// <summary>Runs a command without a shell, streams its output, enforces the timeout and kills the whole process tree.</summary>
public sealed class ProcessRunner : IRunner
{
    private static readonly string[] InheritedVariables =
    [
        "PATH", "HOME", "USER", "USERPROFILE", "TEMP", "TMP", "TMPDIR", "SystemRoot", "LANG", "LC_ALL", "DOTNET_ROOT", "DOTNET_CLI_HOME",
    ];

    private const int MaxLineLength = 8 * 1024;
    private const long MaxTotalBytes = 4 * 1024 * 1024;

    private static readonly TimeSpan DrainLimit = TimeSpan.FromSeconds(2);

    private readonly SandboxLevel _level;

    public ProcessRunner() : this(LinuxSandbox.Detect())
    {
    }

    internal ProcessRunner(SandboxLevel level) => _level = level;

    public SandboxLevel Level => _level;

    public async Task<RunResult> RunAsync(RunRequest request, IProgress<OutputLine> output, CancellationToken cancellationToken)
    {
        if (request.Command.Count == 0)
        {
            throw new ArgumentException("The command is empty.", nameof(request));
        }

        if (_level != SandboxLevel.Full && !request.AllowUnsandboxed)
        {
            return new RunResult(RunStatus.SandboxUnavailable, null, TimeSpan.Zero, $"The sandbox on this computer is incomplete. Missing: {_level.Missing()}. The run needs the user's explicit permission to go ahead without them.");
        }

        var scratch = _level.HasFlag(SandboxLevel.Filesystem) ? CreateScratch() : null;
        try
        {
            return await RunCoreAsync(request, scratch, output, cancellationToken);
        }
        finally
        {
            DeleteScratch(scratch);
        }
    }

    private async Task<RunResult> RunCoreAsync(RunRequest request, string? scratch, IProgress<OutputLine> output, CancellationToken cancellationToken)
    {
        using var process = new Process { StartInfo = StartInfo(request, scratch) };
        var clock = Stopwatch.StartNew();
        try
        {
            process.Start();
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or IOException)
        {
            return new RunResult(RunStatus.FailedToStart, null, clock.Elapsed, ex.Message);
        }

        var pid = process.Id;

        var budget = new OutputBudget();
        var pumps = Task.WhenAll(
            Pump(process.StandardOutput, OutputChannel.Stdout, output, budget),
            Pump(process.StandardError, OutputChannel.Stderr, output, budget),
            Feed(process, request.InputPath));
        using var timeout = new CancellationTokenSource(request.Timeout);
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
        var status = RunStatus.Completed;
        try
        {
            await process.WaitForExitAsync(stop.Token);
        }
        catch (OperationCanceledException)
        {
            status = cancellationToken.IsCancellationRequested ? RunStatus.Cancelled : RunStatus.TimedOut;
            KillAll(process, pid);
            await process.WaitForExitAsync(CancellationToken.None);
        }

        clock.Stop();
        if (_level != SandboxLevel.None)
        {
            LinuxSandbox.KillGroup(pid);
        }

        // A grandchild that survived could keep the pipes open forever, so draining is bounded.
        await Task.WhenAny(pumps, Task.Delay(DrainLimit, CancellationToken.None));
        return new RunResult(status, status == RunStatus.Completed ? process.ExitCode : null, clock.Elapsed);
    }

    private ProcessStartInfo StartInfo(RunRequest request, string? scratch)
    {
        var info = new ProcessStartInfo(request.Command[0])
        {
            WorkingDirectory = request.WorkingDirectory,
            RedirectStandardInput = request.InputPath is not null,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        // The solution must not see the app's environment, which may hold tokens of other tools.
        info.Environment.Clear();
        foreach (var name in InheritedVariables)
        {
            if (Environment.GetEnvironmentVariable(name) is { } value)
            {
                info.Environment[name] = value;
            }
        }

        if (_level == SandboxLevel.None)
        {
            foreach (var arg in request.Command.Skip(1))
            {
                info.ArgumentList.Add(arg);
            }

            return info;
        }

        // The sandbox helper is this very program started with the plan; it confines itself and then executes the command.
        var host = Environment.ProcessPath ?? throw new InvalidOperationException("The path of the running program is unknown, so the sandbox helper cannot be started.");
        info.FileName = host;
        if (Path.GetFileNameWithoutExtension(host) == "dotnet")
        {
            info.ArgumentList.Add(Environment.GetCommandLineArgs()[0]);
        }

        foreach (var arg in BuildPlan(request, scratch, info.Environment.TryGetValue("PATH", out var searchPath) ? searchPath ?? string.Empty : string.Empty).ToArguments())
        {
            info.ArgumentList.Add(arg);
        }

        if (scratch is not null)
        {
            // Toolchains want a writable home and cache; they get the run's private folder instead of the real ones.
            foreach (var name in new[] { "HOME", "TMPDIR", "TMP", "TEMP", "DOTNET_CLI_HOME", "XDG_CACHE_HOME" })
            {
                info.Environment[name] = scratch;
            }
        }

        return info;
    }

    private SandboxPlan BuildPlan(RunRequest request, string? scratch, string path)
    {
        var readable = new List<string>(SystemReadable) { request.WorkingDirectory };
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        // PATH entries hold the toolchains; the home folder itself (or a parent of it) must never be opened up that way.
        foreach (var entry in path.Split(':', StringSplitOptions.RemoveEmptyEntries).Where(Path.IsPathRooted))
        {
            if (!IsSameOrParent(entry, home))
            {
                readable.Add(entry);
            }
        }

        if (ResolveProgram(request.Command[0], path, request.WorkingDirectory) is { } program)
        {
            readable.Add(Path.GetDirectoryName(program)!);
            if (File.ResolveLinkTarget(program, returnFinalTarget: true)?.FullName is { } real)
            {
                readable.Add(Path.GetDirectoryName(real)!);
            }
        }

        readable.AddRange(request.ReadablePaths ?? []);
        var writable = new List<string>();
        if (scratch is not null)
        {
            writable.Add(scratch);
        }

        writable.AddRange(request.WritablePaths ?? []);
        return new SandboxPlan(
            readable,
            writable,
            request.Limits ?? SandboxLimits.Default,
            _level.HasFlag(SandboxLevel.Filesystem),
            _level.HasFlag(SandboxLevel.Network) && !request.AllowNetwork,
            request.Command);
    }

    private static readonly string[] SystemReadable =
        [.. new[] { "/usr", "/lib", "/lib32", "/lib64", "/libx32", "/bin", "/sbin", "/etc", "/opt", "/sys", "/snap", "/nix" }.Where(Directory.Exists)];

    private static bool IsSameOrParent(string candidate, string folder)
    {
        var relative = Path.GetRelativePath(candidate, folder);
        return relative == "." || (!relative.StartsWith("..", StringComparison.Ordinal) && !Path.IsPathRooted(relative));
    }

    private static string? ResolveProgram(string command, string path, string workingDirectory)
    {
        if (command.Contains('/', StringComparison.Ordinal))
        {
            return Path.GetFullPath(command, workingDirectory);
        }

        return path.Split(':', StringSplitOptions.RemoveEmptyEntries).Select(d => Path.Combine(d, command)).FirstOrDefault(File.Exists);
    }

    private static string CreateScratch()
    {
        var dir = Path.Combine(Path.GetTempPath(), "aoc-run-" + Guid.NewGuid().ToString("N"));
        if (OperatingSystem.IsWindows())
        {
            Directory.CreateDirectory(dir);
        }
        else
        {
            Directory.CreateDirectory(dir, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }

        return dir;
    }

    private static void DeleteScratch(string? dir)
    {
        try
        {
            if (dir is not null)
            {
                Directory.Delete(dir, recursive: true);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // A program that locked its own files down leaves its private folder behind; that is not an error of the run.
        }
    }

    // Output is read in chunks so a program printing a huge line or endless output cannot exhaust memory;
    // past the total cap the pipe is still drained (so the program is not blocked) but nothing is reported.
    private static async Task Pump(StreamReader reader, OutputChannel stream, IProgress<OutputLine> output, OutputBudget budget)
    {
        var buffer = new char[4096];
        var line = new StringBuilder();
        var clipped = false;
        int read;
        while ((read = await reader.ReadAsync(buffer)) > 0)
        {
            foreach (var c in buffer.AsSpan(0, read))
            {
                if (c == '\n')
                {
                    if (line.Length > 0 && line[^1] == '\r' && !clipped)
                    {
                        line.Length--;
                    }

                    Emit(line, clipped);
                    clipped = false;
                }
                else if (line.Length < MaxLineLength)
                {
                    line.Append(c);
                }
                else
                {
                    clipped = true;
                }
            }
        }

        if (line.Length > 0)
        {
            Emit(line, clipped);
        }

        void Emit(StringBuilder text, bool wasClipped)
        {
            var length = text.Length + 1;
            text.Append(wasClipped ? "\u2026" : string.Empty);
            var verdict = budget.Take(length);
            if (verdict == Budget.Within)
            {
                output.Report(new OutputLine(stream, text.ToString()));
            }
            else if (verdict == Budget.JustExceeded)
            {
                output.Report(new OutputLine(OutputChannel.Stderr, "Output limit reached; further output is not shown."));
            }

            text.Clear();
        }
    }

    private enum Budget { Within, JustExceeded, Over }

    private sealed class OutputBudget
    {
        private long _used;

        public Budget Take(int length)
        {
            var before = Interlocked.Add(ref _used, length) - length;
            return before >= MaxTotalBytes ? Budget.Over : before + length >= MaxTotalBytes ? Budget.JustExceeded : Budget.Within;
        }
    }

    private static async Task Feed(Process process, string? inputPath)
    {
        if (inputPath is null)
        {
            return;
        }

        try
        {
            await using var file = File.OpenRead(inputPath);
            await file.CopyToAsync(process.StandardInput.BaseStream);
            process.StandardInput.Close();
        }
        catch (IOException)
        {
            // The program may exit without reading its input; that is not an error of the run.
        }
    }

    private void KillAll(Process process, int pid)
    {
        if (_level != SandboxLevel.None)
        {
            LinuxSandbox.KillGroup(pid);
        }

        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
            // Already exited.
        }
    }
}
