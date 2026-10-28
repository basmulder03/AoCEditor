using System.Text.RegularExpressions;

namespace Diagnostics.Domain;

/// <summary>Removes secrets and personal paths from text before it is stored, shown or copied.</summary>
public sealed partial class Redactor
{
    public const string Mask = "[redacted]";

    private readonly Regex? _home;

    public Redactor(string? homeDirectory)
    {
        var home = homeDirectory?.TrimEnd('/', '\\');
        if (!string.IsNullOrEmpty(home))
        {
            var pattern = Regex.Escape(home.Replace('\\', '/')).Replace("/", @"[\\/]", StringComparison.Ordinal);
            _home = new Regex(pattern + @"(?![\w])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
        }
    }

    public string Apply(string text)
    {
        if (text.Length == 0)
        {
            return text;
        }

        text = HeaderLine().Replace(text, "$1: " + Mask);
        text = JsonPair().Replace(text, "$1\"" + Mask + "\"");
        text = Pair().Replace(text, "$1=" + Mask);
        text = LongHex().Replace(text, Mask);
        return _home is null ? text : _home.Replace(text, "~");
    }

    [GeneratedRegex(@"\b(cookie|set-cookie|authorization)\s*:[^\r\n]*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 1000)]
    private static partial Regex HeaderLine();

    [GeneratedRegex("(\"(?:session|token|cookie)\"\\s*:\\s*)\"[^\"]*\"", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 1000)]
    private static partial Regex JsonPair();

    [GeneratedRegex(@"\b(session|token|cookie)=(?!\[redacted\])[^\s;,&""']*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 1000)]
    private static partial Regex Pair();

    [GeneratedRegex(@"(?<![0-9A-Za-z])[0-9a-fA-F]{32,}(?![0-9A-Za-z])", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex LongHex();
}
