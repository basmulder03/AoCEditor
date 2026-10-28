using Shared;
using Shared.Contracts;
using Workspace.Domain;

namespace Workspace.Application;

public enum SetRootResult { Ok, NotAbsolute, OverlapsAppData }

/// <summary>The solutions workspace: its root and the files scaffolded into it on user action.</summary>
public sealed class WorkspaceService(IKeyValueStore settings, IAppDataPaths appData)
{
    private const string RootKey = "workspace.root";

    public Task<string?> RootAsync(CancellationToken cancellationToken) => settings.GetAsync(RootKey, cancellationToken);

    public async Task<SetRootResult> SetRootAsync(string path, CancellationToken cancellationToken)
    {
        if (!Path.IsPathFullyQualified(path))
        {
            return SetRootResult.NotAbsolute;
        }

        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
        if (!DataRootValidator.IsAllowed(appData.Root, root))
        {
            return SetRootResult.OverlapsAppData;
        }

        Directory.CreateDirectory(root);
        await settings.SetAsync(RootKey, root, cancellationToken);
        return SetRootResult.Ok;
    }

    /// <summary>Writes the template files that do not exist yet; existing files are never touched. Returns the files created.</summary>
    public async Task<IReadOnlyList<string>> ScaffoldDayAsync(int year, int day, string language, IReadOnlyDictionary<string, string> files, CancellationToken cancellationToken)
    {
        var root = await RootAsync(cancellationToken) ?? throw new InvalidOperationException("No solutions folder is chosen yet.");
        var folder = WorkspaceLayout.DayFolder(root, year, day, language);
        if (files.Keys.FirstOrDefault(n => !Shared.FileNames.IsPlain(n)) is { } bad)
        {
            throw new ArgumentException($"'{bad}' is not a plain file name.", nameof(files));
        }

        Directory.CreateDirectory(folder);
        var created = new List<string>();
        foreach (var (name, content) in files)
        {
            var path = Path.GetFullPath(Path.Combine(folder, name));
            if (!path.StartsWith(Path.GetFullPath(folder) + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            {
                throw new ArgumentException($"'{name}' would leave the day folder.", nameof(files));
            }

            if (!File.Exists(path))
            {
                await File.WriteAllTextAsync(path, content, cancellationToken);
                created.Add(path);
            }
        }

        return created;
    }

    /// <summary>Languages that already have a folder for the day, sorted.</summary>
    public async Task<IReadOnlyList<string>> LanguagesAsync(int year, int day, CancellationToken cancellationToken)
    {
        var root = await RootAsync(cancellationToken);
        if (root is null)
        {
            return [];
        }

        var dayFolder = Path.GetDirectoryName(WorkspaceLayout.DayFolder(root, year, day, "x"))!;
        return Directory.Exists(dayFolder)
            ? [.. Directory.EnumerateDirectories(dayFolder).Select(d => Path.GetFileName(d)).Order(StringComparer.Ordinal)]
            : [];
    }
}
