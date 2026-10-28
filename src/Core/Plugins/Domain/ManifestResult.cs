namespace AoCEditor.Core.Plugins.Domain;

public sealed record ManifestResult(PluginManifest? Manifest, IReadOnlyList<string> Errors);
