namespace AoCEditor.Core.Runner.Application;

/// <summary>MemoryBytes caps writable memory, CpuSeconds the processor time per process, ExtraTasks how many processes and threads beyond those the user already has, FileBytes the size of any file written.</summary>
public sealed record SandboxLimits(long MemoryBytes, int CpuSeconds, int ExtraTasks, long FileBytes)
{
    public static SandboxLimits Default { get; } = new(4L << 30, 600, 512, 1L << 30);
}
