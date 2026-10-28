using System.Globalization;
using System.Text.RegularExpressions;

namespace Puzzle.Domain;

public enum SubmitOutcome { Unknown, Correct, Wrong, TooHigh, TooLow, RateLimited, AlreadySolved }

/// <summary>Wait is only set for RateLimited, and null when the response gave no readable time.</summary>
public sealed record SubmitResult(SubmitOutcome Outcome, TimeSpan? Wait = null);

public static partial class SubmitResponseParser
{
    [GeneratedRegex(@"You have (?:(\d+)m )?(?:(\d+)s )?left to wait", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex Wait();

    public static SubmitResult Parse(string html)
    {
        var article = HtmlNode.Parse(html).Descendants().FirstOrDefault(n => n.Name == "article");
        if (article is null)
        {
            return new SubmitResult(SubmitOutcome.Unknown);
        }

        var text = article.PlainText();
        if (text.Contains("You gave an answer too recently", StringComparison.Ordinal))
        {
            var m = Wait().Match(text);
            var wait = m.Success && (m.Groups[1].Success || m.Groups[2].Success)
                ? TimeSpan.FromMinutes(Number(m.Groups[1])) + TimeSpan.FromSeconds(Number(m.Groups[2]))
                : (TimeSpan?)null;
            return new SubmitResult(SubmitOutcome.RateLimited, wait);
        }

        var outcome =
            text.Contains("That's the right answer", StringComparison.Ordinal) ? SubmitOutcome.Correct :
            text.Contains("not the right answer", StringComparison.Ordinal) ? WrongKind(text) :
            text.Contains("Did you already complete it", StringComparison.Ordinal) ? SubmitOutcome.AlreadySolved :
            SubmitOutcome.Unknown;
        return new SubmitResult(outcome);
    }

    private static SubmitOutcome WrongKind(string text) =>
        text.Contains("too high", StringComparison.Ordinal) ? SubmitOutcome.TooHigh :
        text.Contains("too low", StringComparison.Ordinal) ? SubmitOutcome.TooLow :
        SubmitOutcome.Wrong;

    private static int Number(Group g) => g.Success ? int.Parse(g.Value, CultureInfo.InvariantCulture) : 0;
}
