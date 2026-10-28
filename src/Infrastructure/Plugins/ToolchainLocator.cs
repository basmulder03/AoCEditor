namespace AoCEditor.Infrastructure.Plugins;

/// <summary>Finds a plugin's toolchain executable on PATH; detection only, nothing is downloaded or run.</summary>
public static class ToolchainLocator
{
    private static readonly string[] WindowsExtensions = [".exe", ".cmd", ".bat"];

    public static string? Find(IEnumerable<string> executableNames, string? pathVariable)
    {
        if (string.IsNullOrEmpty(pathVariable))
        {
            return null;
        }

        // A relative PATH entry would resolve against the current folder and could pick up a planted program.
        var dirs = pathVariable.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries).Where(Path.IsPathFullyQualified);
        foreach (var name in executableNames.Where(n => n.IndexOfAny(['/', '\\']) < 0))
        {
            foreach (var dir in dirs)
            {
                var hit = Candidates(dir, name).FirstOrDefault(IsExecutable);
                if (hit is not null)
                {
                    return hit;
                }
            }
        }

        return null;
    }

    private static IEnumerable<string> Candidates(string dir, string name) =>
        OperatingSystem.IsWindows() ? WindowsExtensions.Select(e => Path.Combine(dir, name + e)) : [Path.Combine(dir, name)];

    private static bool IsExecutable(string path) =>
        File.Exists(path) && (OperatingSystem.IsWindows() || (File.GetUnixFileMode(path) & (UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute)) != 0);
}
