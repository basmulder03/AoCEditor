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

    public SandboxLevel Level => SandboxLevel.None;

    public async Task<RunResult> RunAsync(RunRequest request, IProgress<OutputLine> output, CancellationToken cancellationToken)
    {
        if (request.Command.Count == 0)
        {
            throw new ArgumentException("The command is empty.", nameof(request));
        }

        if (!request.AllowUnsandboxed)
        {
            return new RunResult(RunStatus.SandboxUnavailable, null, TimeSpan.Zero, "No sandbox is available; the run needs the user's explicit permission to go unsandboxed.");
        }

        using var process = new Process { StartInfo = StartInfo(request) };
        var clock = Stopwatch.StartNew();
        try
        {
            process.Start();
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or IOException)
        {
            return new RunResult(RunStatus.FailedToStart, null, clock.Elapsed, ex.Message);
        }

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
            Kill(process);
            await process.WaitForExitAsync(CancellationToken.None);
        }

        clock.Stop();
        // A grandchild that survived could keep the pipes open forever, so draining is bounded.
        await Task.WhenAny(pumps, Task.Delay(DrainLimit, CancellationToken.None));
        return new RunResult(status, status == RunStatus.Completed ? process.ExitCode : null, clock.Elapsed);
    }

    private static ProcessStartInfo StartInfo(RunRequest request)
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
        foreach (var arg in request.Command.Skip(1))
        {
            info.ArgumentList.Add(arg);
        }

        // The solution must not see the app's environment, which may hold tokens of other tools.
        info.Environment.Clear();
        foreach (var name in InheritedVariables)
        {
            if (Environment.GetEnvironmentVariable(name) is { } value)
            {
                info.Environment[name] = value;
            }
        }

        return info;
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

    private static void Kill(Process process)
    {
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
