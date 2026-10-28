namespace Plugins.Domain;

public sealed record PluginManifest(
    string Id,
    string Name,
    IReadOnlyList<string> Extensions,
    IReadOnlyDictionary<string, string> TemplateFiles,
    IReadOnlyList<string> ToolchainExecutables,
    CommandSpec? Compile,
    CommandSpec Run);
