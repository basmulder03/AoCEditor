using System.Text.Json;
using System.Text.Json.Serialization;
using Shared;

namespace AoCEditor.Core.Plugins.Domain;

[JsonSerializable(typeof(ManifestDto))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true)]
internal sealed partial class ManifestJson : JsonSerializerContext;
