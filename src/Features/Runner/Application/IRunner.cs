namespace Runner.Application;

/// <summary>Starts user code; the console and timing features depend on this, not on a concrete runner.</summary>
public interface IRunner
{
    SandboxLevel Level { get; }

    Task<RunResult> RunAsync(RunRequest request, IProgress<OutputLine> output, CancellationToken cancellationToken);
}
