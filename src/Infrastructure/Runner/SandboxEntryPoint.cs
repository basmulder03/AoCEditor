namespace AoCEditor.Infrastructure.Runner;

/// <summary>
/// Entry point of the re-executed helper: the host calls this first thing in Main. When the arguments ask for it, the
/// helper confines itself and replaces itself with the program, so nothing of the host's own code runs afterwards.
/// </summary>
public static class SandboxEntryPoint
{
    internal const int SetupFailed = 125;
    internal const string RefusalPrefix = "Sandbox refused to start";
    private const int ExecFailed = 127;

    /// <summary>Null when the arguments are not for the helper. Otherwise the exit code, which is only reached when the program could not be started.</summary>
    public static int? RunIfRequested(IReadOnlyList<string> args)
    {
        SandboxPlan? plan;
        try
        {
            plan = SandboxPlan.TryParse(args);
        }
        catch (FormatException ex)
        {
            Console.Error.WriteLine($"{RefusalPrefix}: {ex.Message}");
            return SetupFailed;
        }

        if (plan is null)
        {
            return null;
        }

        var error = OperatingSystem.IsLinux() ? LinuxSandbox.Apply(plan) : "only Linux is supported";
        if (error is not null)
        {
            Console.Error.WriteLine($"{RefusalPrefix} the program: {error}.");
            return SetupFailed;
        }

        Console.Error.WriteLine($"Could not start '{plan.Command[0]}' (error {LinuxSandbox.Exec(plan.Command)}).");
        return ExecFailed;
    }
}
