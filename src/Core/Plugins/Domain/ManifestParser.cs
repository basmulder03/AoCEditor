using System.Text.Json;
using System.Text.Json.Serialization;
using Shared;

namespace AoCEditor.Core.Plugins.Domain;

/// <summary>Reads and validates a <c>plugin.json</c>; a manifest with any error is rejected whole.</summary>
public static class ManifestParser
{
    public const int SchemaVersion = 1;

    public static ManifestResult Parse(string json)
    {
        ManifestDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize(json, ManifestJson.Default.ManifestDto);
        }
        catch (JsonException ex)
        {
            return Rejected($"The manifest is not valid JSON: {ex.Message}");
        }

        if (dto is null)
        {
            return Rejected("The manifest is not valid JSON: it is empty.");
        }

        if (dto.SchemaVersion != SchemaVersion)
        {
            return Rejected($"Unsupported schema version {dto.SchemaVersion?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "(missing)"}; this app reads version {SchemaVersion}.");
        }

        var errors = new List<string>();
        if (!IsLanguageId(dto.Id))
        {
            errors.Add("The id must be lowercase letters, digits, '-' and '_'.");
        }

        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            errors.Add("The name is missing.");
        }

        if (dto.Extensions is not { Length: > 0 } || dto.Extensions.Any(e => e.Length < 2 || e[0] != '.' || e.Contains('/') || e.Contains('\\')))
        {
            errors.Add("Each extension must start with a dot, like \".py\".");
        }

        var templates = dto.TemplateFiles ?? [];
        if (templates.Count == 0)
        {
            errors.Add("The plugin needs at least one template file.");
        }

        errors.AddRange(templates.Keys.Where(n => !FileNames.IsPlain(n)).Select(n => $"The template file name '{n}' must be a plain file name."));

        var executables = dto.Toolchain?.Executables ?? [];
        if (executables.Length == 0 || executables.Any(e => !FileNames.IsPlain(e)))
        {
            errors.Add("The toolchain needs at least one executable name without a path.");
        }

        var run = Command(dto.Run, "run", errors, required: true);
        var compile = Command(dto.Compile, "compile", errors, required: false);
        return errors.Count > 0
            ? new ManifestResult(null, errors)
            : new ManifestResult(new PluginManifest(dto.Id!, dto.Name!, dto.Extensions!, templates, executables, compile, run!), []);
    }

    private static ManifestResult Rejected(string error) => new(null, [error]);

    private static CommandSpec? Command(CommandDto? dto, string label, List<string> errors, bool required)
    {
        if (dto is null)
        {
            if (required)
            {
                errors.Add($"The {label} command is missing.");
            }

            return null;
        }

        if (dto.Command is not { Length: > 0 })
        {
            errors.Add($"The {label} command needs at least the program to start.");
            return null;
        }

        foreach (var unknown in CommandTemplate.UnknownPlaceholders(dto.Command))
        {
            errors.Add($"The {label} command uses the unknown placeholder {{{unknown}}}.");
        }

        var input = (dto.Input ?? "stdin") switch
        {
            "stdin" => InputMode.Stdin,
            "file" => InputMode.File,
            _ => (InputMode?)null,
        };
        if (input is null)
        {
            errors.Add($"The {label} input must be \"stdin\" or \"file\".");
        }

        return input is { } mode ? new CommandSpec(dto.Command, mode) : null;
    }

    private static bool IsLanguageId(string? id) =>
        !string.IsNullOrEmpty(id) && id.All(c => c is (>= 'a' and <= 'z') or (>= '0' and <= '9') or '_' or '-') && id[0] is not ('-' or '_');
}
