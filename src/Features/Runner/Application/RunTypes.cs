namespace Runner.Application;

/// <summary>How much isolation a run gets; only <see cref="None"/> exists until the OS sandboxes are built.</summary>
public enum SandboxLevel { None }

public enum RunStatus { Completed, TimedOut, Cancelled, FailedToStart, SandboxUnavailable }

public enum OutputChannel { Stdout, Stderr }

public sealed record OutputLine(OutputChannel Stream, string Text);

/// <summary>Command is the already expanded program and arguments; InputPath is piped to stdin when set.</summary>
public sealed record RunRequest(
    IReadOnlyList<string> Command,
    string WorkingDirectory,
    string? InputPath,
    TimeSpan Timeout,
    bool AllowUnsandboxed = false);

/// <summary>Elapsed is the wall-clock time of the run as measured by the runner; ExitCode is null unless the program ended on its own.</summary>
public sealed record RunResult(RunStatus Status, int? ExitCode, TimeSpan Elapsed, string? Error = null);
