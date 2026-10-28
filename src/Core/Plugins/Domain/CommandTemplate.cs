using System.Text.RegularExpressions;

namespace Plugins.Domain;

public static partial class CommandTemplate
{
    public static readonly IReadOnlySet<string> Placeholders = new HashSet<string> { "toolchain", "source", "input", "workdir" };

    [GeneratedRegex(@"\{([a-z]+)\}", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex Placeholder();

    /// <summary>Fills each argument in one pass, so a value is never read as another placeholder.</summary>
    public static IReadOnlyList<string> Expand(IReadOnlyList<string> template, IReadOnlyDictionary<string, string> values) =>
        [.. template.Select(arg => Placeholder().Replace(arg, m =>
            values.TryGetValue(m.Groups[1].Value, out var v) ? v : throw new ArgumentException($"No value for {{{m.Groups[1].Value}}}.", nameof(values))))];

    internal static IEnumerable<string> UnknownPlaceholders(IEnumerable<string> template) =>
        template.SelectMany(arg => Placeholder().Matches(arg).Select(m => m.Groups[1].Value)).Where(p => !Placeholders.Contains(p)).Distinct();
}
