using System.Text.Json;
using System.Text.Json.Serialization;
using Shared;

namespace AoCEditor.Core.Plugins.Domain;

internal sealed class CommandDto
{
    public string[]? Command { get; set; }

    public string? Input { get; set; }
}
