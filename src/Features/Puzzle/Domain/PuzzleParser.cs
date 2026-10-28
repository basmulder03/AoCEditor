using System.Text;

namespace Puzzle.Domain;

/// <summary>Turns a puzzle page into the document model; returns null when the page has no puzzle article.</summary>
public static class PuzzleParser
{
    private const string AnswerPrefix = "Your puzzle answer was";

    public static PuzzleDocument? Parse(string html)
    {
        var root = HtmlNode.Parse(html);
        var articles = root.Descendants().Where(IsArticle).ToList();
        if (articles.Count == 0)
        {
            return null;
        }

        var answers = root.Descendants()
            .Where(n => n.Name == "p" && n.PlainText().StartsWith(AnswerPrefix, StringComparison.Ordinal))
            .Select(n => n.Descendants().FirstOrDefault(d => d.Name == "code")?.PlainText())
            .OfType<string>()
            .ToList();

        return new PuzzleDocument(TitleOf(articles[0]), [.. articles.Select(a => new PuzzlePart(BlocksOf(a)))], answers);
    }

    private static bool IsArticle(HtmlNode n) =>
        n.Name == "article" && n.Attrs.TryGetValue("class", out var c) && c.Split(' ').Contains("day-desc");

    private static string TitleOf(HtmlNode article)
    {
        var heading = article.Descendants().FirstOrDefault(n => n.Name == "h2")?.PlainText().Trim('-', ' ') ?? string.Empty;
        var colon = heading.IndexOf(':', StringComparison.Ordinal);
        return heading.StartsWith("Day ", StringComparison.Ordinal) && colon > 0 ? heading[(colon + 1)..].Trim() : heading;
    }

    private static List<Block> BlocksOf(HtmlNode article)
    {
        var blocks = new List<Block>();
        foreach (var child in article.Children.Where(c => c.Name != "h2"))
        {
            switch (child.Name)
            {
                case "ul" or "ol":
                    blocks.Add(new ListBlock([.. child.Children.Where(li => li.Name == "li").Select(Inlines)]));
                    break;
                case "pre":
                    blocks.Add(new CodeBlock(child.PlainText()));
                    break;
                default:
                    var inlines = Inlines(child);
                    if (inlines.Any(i => i.Text.Trim().Length > 0))
                    {
                        blocks.Add(new ParagraphBlock(inlines));
                    }

                    break;
            }
        }

        return blocks;
    }

    private static List<Inline> Inlines(HtmlNode node)
    {
        if (node.IsText)
        {
            return [new Inline(Collapse(node.Text!))];
        }

        var style = node.Name switch
        {
            "code" => InlineStyle.Code,
            "em" => node.Descendants().Any(d => d.Name == "code") ? InlineStyle.Code : InlineStyle.Emphasis,
            "a" => InlineStyle.Link,
            _ => InlineStyle.Plain,
        };
        if (style != InlineStyle.Plain)
        {
            node.Attrs.TryGetValue("href", out var href);
            return [new Inline(Collapse(node.PlainText()), style, style == InlineStyle.Link ? href : null)];
        }

        return [.. node.Children.SelectMany(Inlines)];
    }

    private static string Collapse(string text)
    {
        var sb = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            if (!char.IsWhiteSpace(c))
            {
                sb.Append(c);
            }
            else if (sb.Length == 0 || sb[^1] != ' ')
            {
                sb.Append(' ');
            }
        }

        return sb.ToString();
    }
}
