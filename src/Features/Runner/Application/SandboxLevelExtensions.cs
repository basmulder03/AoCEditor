namespace Runner.Application;

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
