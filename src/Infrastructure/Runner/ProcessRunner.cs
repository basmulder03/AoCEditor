using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using AoCEditor.Core.Runner.Application;

namespace AoCEditor.Infrastructure.Runner;

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
    private readonly string[] _protectedFolders;

    public ProcessRunner() : this(LinuxSandbox.Detect())
    {
    }

    /// <summary>The folders (the app-data folder, for one) that no PATH entry or toolchain location may ever make readable.</summary>
    public ProcessRunner(IReadOnlyList<string> protectedFolders) : this(LinuxSandbox.Detect(), protectedFolders)
    {
    }

    internal ProcessRunner(SandboxLevel level, IReadOnlyList<string>? protectedFolders = null)
    {
        _level = level;
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        _protectedFolders = [.. (protectedFolders ?? []).Concat([Path.Combine(home, ".ssh"), Path.Combine(home, ".config")]).Select(Canonical)];
    }

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
        var helper = new HelperWatch(output);
        output = helper;
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
            PumpAsync(process.StandardOutput, OutputChannel.Stdout, output, budget),
            PumpAsync(process.StandardError, OutputChannel.Stderr, output, budget),
            FeedAsync(process, request.InputPath));
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
        if (status == RunStatus.Completed && _level != SandboxLevel.None && process.ExitCode == SandboxEntryPoint.SetupFailed && helper.Refusal is { } refusal)
        {
            return new RunResult(RunStatus.SandboxFailed, null, clock.Elapsed, $"The sandbox could not be set up, so the program was not started. {refusal}");
        }

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

    internal SandboxPlan BuildPlan(RunRequest request, string? scratch, string path)
    {
        var readable = new List<string>(SystemReadable) { request.WorkingDirectory };
        // PATH entries hold the toolchains; they are resolved through symlinks first so a link cannot smuggle in the home folder or a secret folder.
        foreach (var entry in path.Split(':', StringSplitOptions.RemoveEmptyEntries).Where(Path.IsPathRooted))
        {
            AddGrantable(readable, entry);
        }

        if (ResolveProgram(request.Command[0], path, request.WorkingDirectory) is { } program)
        {
            AddGrantable(readable, Path.GetDirectoryName(program)!);
            if (File.ResolveLinkTarget(program, returnFinalTarget: true)?.FullName is { } real)
            {
                AddGrantable(readable, Path.GetDirectoryName(real)!);
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
            _level.HasFlag(SandboxLevel.Network),
            request.AllowNetwork,
            request.Command);
    }

    private void AddGrantable(List<string> readable, string folder)
    {
        var real = Canonical(folder);
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var forbidden = new[] { Canonical(home) }.Concat(_protectedFolders).Any(p => IsSameOrParent(real, p)) || _protectedFolders.Any(p => IsSameOrParent(p, real));
        if (!forbidden)
        {
            readable.Add(real);
        }
    }

    // Resolves symlinks in every component, so the check sees where the path really leads.
    private static string Canonical(string path)
    {
        var resolved = Path.GetPathRoot(Path.GetFullPath(path)) ?? "/";
        foreach (var part in Path.GetFullPath(path)[resolved.Length..].Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries))
        {
            resolved = Path.Combine(resolved, part);
            try
            {
                if (Directory.ResolveLinkTarget(resolved, returnFinalTarget: true) is { } target)
                {
                    resolved = target.FullName;
                }
            }
            catch (IOException)
            {
                // Not resolvable (loop or vanished): keep the lexical path, which only makes the check stricter.
            }
        }

        return resolved;
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
    private static async Task PumpAsync(StreamReader reader, OutputChannel stream, IProgress<OutputLine> output, OutputBudget budget)
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

    // Remembers the helper's refusal line so exit code 125 can be told apart from a program that exits with 125.
    private sealed class HelperWatch(IProgress<OutputLine> inner) : IProgress<OutputLine>
    {
        public string? Refusal { get; private set; }

        public void Report(OutputLine value)
        {
            if (value.Stream == OutputChannel.Stderr && value.Text.StartsWith(SandboxEntryPoint.RefusalPrefix, StringComparison.Ordinal))
            {
                Refusal = value.Text;
            }

            inner.Report(value);
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

    private static async Task FeedAsync(Process process, string? inputPath)
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
