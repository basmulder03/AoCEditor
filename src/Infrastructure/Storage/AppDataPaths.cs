using Shared.Contracts;

namespace AoCEditor.Infrastructure.Storage;

public sealed class AppDataPaths(string root) : IAppDataPaths
{
    public AppDataPaths() : this(DefaultRoot())
    {
    }

    public string Root => Ensure(root);

    public string Cache => Ensure(Path.Combine(root, "cache"));

    public string Database => Path.Combine(Root, "aoceditor.db");

    // LocalApplicationData honours XDG_DATA_HOME on Linux and is %LOCALAPPDATA% on Windows.
    public static string DefaultRoot() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AoCEditor");

    private static string Ensure(string path)
    {
        Directory.CreateDirectory(path);
        return path;
    }
}
