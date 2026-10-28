namespace Shared;

public static class DataRootValidator
{
    // shortcut: compares resolved paths textually; symlinks are not followed.
    public static bool IsAllowed(string dataRoot, string solutionsFolder)
    {
        var data = Normalize(dataRoot);
        var solutions = Normalize(solutionsFolder);
        var cmp = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return !data.StartsWith(solutions, cmp) && !solutions.StartsWith(data, cmp);
    }

    private static string Normalize(string path) =>
        Path.TrimEndingDirectorySeparator(Path.GetFullPath(path)) + Path.DirectorySeparatorChar;
}
