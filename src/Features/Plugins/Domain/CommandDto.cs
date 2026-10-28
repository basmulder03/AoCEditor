using Shared;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Plugins.Domain;

internal sealed class CommandDto
{
    public string[]? Command { get; set; }

    public string? Input { get; set; }
}
