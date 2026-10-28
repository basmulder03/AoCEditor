namespace Runner.Application;

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
