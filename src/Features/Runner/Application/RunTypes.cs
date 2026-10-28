namespace Runner.Application;

/// <summary>The protections a run gets: resource limits, filesystem confinement and no network. <see cref="Full"/> is the only level that needs no consent.</summary>
[Flags]
public enum SandboxLevel { None = 0, Limits = 1, Filesystem = 2, Network = 4, Full = 7 }

public static class SandboxLevelExtensions
{
    /// <summary>What a build step may do beyond a run step; shown wherever the user is asked to accept the sandbox.</summary>
    public const string BuildStepNotice = "The build step may use the internet (internet sockets only, for package restore) and write into the day's folder.";

    /// <summary>Names the protections the level lacks, or an empty string for <see cref="SandboxLevel.Full"/>.</summary>
    public static string Missing(this SandboxLevel level)
    {
        var missing = new List<string>();
        if (!level.HasFlag(SandboxLevel.Limits))
        {
            missing.Add("resource limits");
        }

        if (!level.HasFlag(SandboxLevel.Filesystem))
        {
            missing.Add("filesystem isolation");
        }

        if (!level.HasFlag(SandboxLevel.Network))
        {
            missing.Add("network isolation");
        }

        return string.Join(", ", missing);
    }
}

/// <summary>MemoryBytes caps writable memory, CpuSeconds the processor time per process, ExtraTasks how many processes and threads beyond those the user already has, FileBytes the size of any file written.</summary>
public sealed record SandboxLimits(long MemoryBytes, int CpuSeconds, int ExtraTasks, long FileBytes)
{
    public static SandboxLimits Default { get; } = new(4L << 30, 600, 512, 1L << 30);
}

public enum RunStatus { Completed, TimedOut, Cancelled, FailedToStart, SandboxUnavailable, SandboxFailed }

public enum OutputChannel { Stdout, Stderr }

public sealed record OutputLine(OutputChannel Stream, string Text);

/// <summary>
/// Command is the already expanded program and arguments; InputPath is piped to stdin when set.
/// ReadablePaths and WritablePaths widen the sandbox's filesystem rules (the working directory is always readable); AllowNetwork lifts the network ban, which only build steps may ask for.
/// </summary>
public sealed record RunRequest(
    IReadOnlyList<string> Command,
    string WorkingDirectory,
    string? InputPath,
    TimeSpan Timeout,
    bool AllowUnsandboxed = false,
    IReadOnlyList<string>? ReadablePaths = null,
    IReadOnlyList<string>? WritablePaths = null,
    bool AllowNetwork = false,
    SandboxLimits? Limits = null);

/// <summary>Elapsed is the wall-clock time of the run as measured by the runner; ExitCode is null unless the program ended on its own.</summary>
public sealed record RunResult(RunStatus Status, int? ExitCode, TimeSpan Elapsed, string? Error = null);
