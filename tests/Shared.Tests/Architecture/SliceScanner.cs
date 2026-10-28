using System.Text.RegularExpressions;

namespace Shared.Tests.Architecture;

/// <summary>Reads the sources under src/Core, src/Infrastructure and src/UI and reports references across slices.</summary>
internal static partial class SliceScanner
{
    private static readonly string[] Layers = ["Core", "Infrastructure", "UI"];

    [GeneratedRegex(@"AoCEditor\.(?:Core|Infrastructure|UI)\.(\w+)")]
    private static partial Regex SliceReference();

    [GeneratedRegex(@"^namespace\s+([\w.]+)\s*;", RegexOptions.Multiline)]
    private static partial Regex NamespaceDeclaration();

    public static string FindRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (!File.Exists(Path.Combine(dir, "AoCEditor.slnx")))
        {
            dir = Path.GetDirectoryName(dir) ?? throw new InvalidOperationException("repository root not found");
        }

        return dir;
    }

    public static IEnumerable<(string File, string Layer, string Slice)> Sources(string srcRoot) =>
        Layers.SelectMany(layer => Directory.EnumerateFiles(Path.Combine(srcRoot, layer), "*.*", SearchOption.AllDirectories)
            .Where(f => f.EndsWith(".cs", StringComparison.Ordinal) || f.EndsWith(".axaml", StringComparison.Ordinal))
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .Select(f => (File: f, Layer: layer, Slice: Path.GetRelativePath(Path.Combine(srcRoot, layer), f).Split(Path.DirectorySeparatorChar)[0])));

    public static List<string> CrossSliceReferences(string srcRoot)
    {
        var found = new List<string>();
        foreach (var (file, _, slice) in Sources(srcRoot))
        {
            foreach (Match match in SliceReference().Matches(File.ReadAllText(file)))
            {
                if (match.Groups[1].Value != slice)
                {
                    found.Add($"{Path.GetRelativePath(srcRoot, file)}: {match.Value} (slice {slice})");
                }
            }
        }

        return found;
    }

    public static List<string> NamespacesNotMirroringFolders(string srcRoot)
    {
        var found = new List<string>();
        foreach (var (file, layer, _) in Sources(srcRoot).Where(s => s.File.EndsWith(".cs", StringComparison.Ordinal)))
        {
            var match = NamespaceDeclaration().Match(File.ReadAllText(file));
            if (!match.Success)
            {
                continue;
            }

            var folders = Path.GetRelativePath(Path.Combine(srcRoot, layer), Path.GetDirectoryName(file)!).Split(Path.DirectorySeparatorChar);
            var expected = string.Join('.', ["AoCEditor", layer, .. folders.Where(f => f != ".")]);
            if (match.Groups[1].Value != expected)
            {
                found.Add($"{Path.GetRelativePath(srcRoot, file)}: namespace {match.Groups[1].Value}, expected {expected}");
            }
        }

        return found;
    }
}
