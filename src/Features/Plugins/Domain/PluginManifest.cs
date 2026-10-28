namespace Plugins.Domain;

public enum InputMode { Stdin, File }

/// <summary>A command as an argument list with <c>{placeholders}</c>; it is never run through a shell.</summary>
public sealed record CommandSpec(IReadOnlyList<string> Command, InputMode Input = InputMode.Stdin);

public sealed record PluginManifest(
    string Id,
    string Name,
    IReadOnlyList<string> Extensions,
    IReadOnlyDictionary<string, string> TemplateFiles,
    IReadOnlyList<string> ToolchainExecutables,
    CommandSpec? Compile,
    CommandSpec Run);

public sealed record ManifestResult(PluginManifest? Manifest, IReadOnlyList<string> Errors);
