namespace AoCEditor.Core.Runner.Application;

/// <summary>The protections a run gets: resource limits, filesystem confinement and no network. <see cref="Full"/> is the only level that needs no consent.</summary>
[Flags]
public enum SandboxLevel { None = 0, Limits = 1, Filesystem = 2, Network = 4, Full = 7 }
