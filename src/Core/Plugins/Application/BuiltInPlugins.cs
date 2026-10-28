using AoCEditor.Core.Plugins.Domain;


namespace AoCEditor.Core.Plugins.Application;

/// <summary>The manifests shipped inside the app, in the same format a third-party plugin uses.</summary>
public static class BuiltInPlugins
{
    public static IReadOnlyList<PluginManifest> Load()
    {
        var assembly = typeof(BuiltInPlugins).Assembly;
        return [.. assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith("BuiltIn/", StringComparison.Ordinal) && n.EndsWith(".json", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .Select(name =>
            {
                using var reader = new StreamReader(assembly.GetManifestResourceStream(name)!);
                var result = ManifestParser.Parse(reader.ReadToEnd());
                return result.Manifest ?? throw new InvalidOperationException($"Built-in plugin {name} is invalid: {string.Join(' ', result.Errors)}");
            })];
    }
}
