using Shared;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Plugins.Domain;

internal sealed class ToolchainDto
{
    public string[]? Executables { get; set; }
}
