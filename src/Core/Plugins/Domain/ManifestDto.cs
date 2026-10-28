using System.Text.Json;
using System.Text.Json.Serialization;
using Shared;

namespace AoCEditor.Core.Plugins.Domain;

internal sealed class ManifestDto
{
    public int? SchemaVersion { get; set; }

    public string? Id { get; set; }

    public string? Name { get; set; }

    public string[]? Extensions { get; set; }

    public Dictionary<string, string>? TemplateFiles { get; set; }

    public ToolchainDto? Toolchain { get; set; }

    public CommandDto? Compile { get; set; }

    public CommandDto? Run { get; set; }
}
