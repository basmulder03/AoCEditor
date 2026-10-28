using System.ComponentModel;
using System.Diagnostics;

namespace Runner.Application;

/// <summary>Runs a command without a shell, streams its output, enforces the timeout and kills the whole process tree.</summary>
public sealed class ProcessRunner : IRunner
{
    private static readonly string[] InheritedVariables =
    [
        "PATH", "HOME", "USER", "USERPROFILE", "TEMP", "TMP", "TMPDIR", "SystemRoot", "LANG", "LC_ALL", "DOTNET_ROOT", "DOTNET_CLI_HOME",
    ];

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

        var pumps = Task.WhenAll(
            Pump(process.StandardOutput, OutputChannel.Stdout, output),
            Pump(process.StandardError, OutputChannel.Stderr, output),
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

    private static async Task Pump(StreamReader reader, OutputChannel stream, IProgress<OutputLine> output)
    {
        while (await reader.ReadLineAsync() is { } line)
        {
            output.Report(new OutputLine(stream, line));
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
