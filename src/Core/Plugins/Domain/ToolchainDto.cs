using System.Text.Json;
using System.Text.Json.Serialization;
using Shared;

namespace AoCEditor.Core.Plugins.Domain;

internal sealed class ToolchainDto
{
    public string[]? Executables { get; set; }
}
