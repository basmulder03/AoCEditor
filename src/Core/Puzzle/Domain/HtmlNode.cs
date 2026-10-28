using System.Net;
using System.Text;

namespace AoCEditor.Core.Puzzle.Domain;

/// <summary>Minimal forgiving HTML tree: enough for AoC pages, not a general parser.</summary>
internal sealed class HtmlNode(string name, string? text = null)
{
    private static readonly HashSet<string> Void = ["br", "hr", "img", "input", "meta", "link"];

    public string Name { get; } = name;
    public string? Text { get; } = text;
    public Dictionary<string, string> Attrs { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<HtmlNode> Children { get; } = [];

    public bool IsText => Text is not null;

    public string PlainText() => IsText ? Text! : string.Concat(Children.Select(c => c.PlainText()));

    public IEnumerable<HtmlNode> Descendants() => Children.SelectMany(c => c.Descendants().Prepend(c));

    public static HtmlNode Parse(string html)
    {
        var root = new HtmlNode("#root");
        var open = new List<HtmlNode> { root };
        var i = 0;
        while (i < html.Length)
        {
            var lt = html.IndexOf('<', i);
            if (lt < 0 || lt + 1 >= html.Length || !(char.IsLetter(html[lt + 1]) || html[lt + 1] is '/' or '!'))
            {
                // a stray '<' is plain text
                var end = lt < 0 ? html.Length : lt + 1;
                open[^1].Children.Add(new HtmlNode("#text", WebUtility.HtmlDecode(html[i..end])));
                i = end;
                continue;
            }

            if (lt > i)
            {
                open[^1].Children.Add(new HtmlNode("#text", WebUtility.HtmlDecode(html[i..lt])));
            }

            var gt = html.IndexOf('>', lt);
            if (gt < 0)
            {
                break;
            }

            i = gt + 1;
            var tag = html[(lt + 1)..gt];
            if (tag.StartsWith("!--", StringComparison.Ordinal))
            {
                var close = html.IndexOf("-->", lt, StringComparison.Ordinal);
                i = close < 0 ? html.Length : close + 3;
            }
            else if (tag[0] == '/')
            {
                var name = tag[1..].Trim().ToLowerInvariant();
                var at = open.FindLastIndex(n => n.Name == name);
                if (at > 0)
                {
                    open.RemoveRange(at, open.Count - at);
                }
            }
            else if (tag[0] != '!')
            {
                var node = StartTag(tag);
                open[^1].Children.Add(node);
                if (!tag.EndsWith('/') && !Void.Contains(node.Name))
                {
                    open.Add(node);
                }
            }
        }

        return root;
    }

    private static HtmlNode StartTag(string tag)
    {
        var nameEnd = tag.IndexOfAny([' ', '\n', '\t', '\r', '/']);
        var node = new HtmlNode((nameEnd < 0 ? tag : tag[..nameEnd]).ToLowerInvariant());
        var rest = nameEnd < 0 ? string.Empty : tag[nameEnd..];
        var p = 0;
        while (p < rest.Length)
        {
            while (p < rest.Length && (char.IsWhiteSpace(rest[p]) || rest[p] == '/'))
            {
                p++;
            }

            var keyStart = p;
            while (p < rest.Length && !char.IsWhiteSpace(rest[p]) && rest[p] is not ('=' or '/'))
            {
                p++;
            }

            var key = rest[keyStart..p];
            var value = new StringBuilder();
            if (p < rest.Length && rest[p] == '=')
            {
                p++;
                var quote = p < rest.Length && rest[p] is '"' or '\'' ? rest[p++] : '\0';
                while (p < rest.Length && (quote == '\0' ? !char.IsWhiteSpace(rest[p]) : rest[p] != quote))
                {
                    value.Append(rest[p++]);
                }

                p++;
            }

            if (key.Length > 0)
            {
                node.Attrs[key] = WebUtility.HtmlDecode(value.ToString());
            }
        }

        return node;
    }
}
