namespace Runner.Application;

/// <summary>Elapsed is the wall-clock time of the run as measured by the runner; ExitCode is null unless the program ended on its own.</summary>
public sealed record RunResult(RunStatus Status, int? ExitCode, TimeSpan Elapsed, string? Error = null);
