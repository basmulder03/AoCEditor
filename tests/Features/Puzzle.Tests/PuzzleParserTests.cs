using Puzzle.Domain;
using Shouldly;
using Xunit;

namespace Puzzle.Tests;

public class PuzzleParserTests
{
    private const string Part1 =
        "<article class=\"day-desc\"><h2>--- Day 3: Toboggan Trajectory ---</h2>"
        + "<p>Count the <em>trees</em> with <code>x+3</code> &amp; see <a href=\"/2020/about\">about</a>.</p>"
        + "<ul><li>one</li><li>two <em>2</em></li></ul>"
        + "<pre><code>..##\n#...\n</code></pre></article>";

    private const string Part2 = "<article class=\"day-desc\"><h2 id=\"part2\">--- Part Two ---</h2><p>Again.</p></article>";

    [Fact]
    public void TitleComesFromTheDayHeading()
    {
        PuzzleParser.Parse(Part1)!.Title.ShouldBe("Toboggan Trajectory");
    }

    [Fact]
    public void BlocksKeepTheirKindAndInlineStyles()
    {
        var blocks = PuzzleParser.Parse(Part1)!.Parts[0].Blocks;

        blocks.Count.ShouldBe(3);
        var p = blocks[0].ShouldBeOfType<ParagraphBlock>();
        p.Inlines.Select(i => (i.Text, i.Style)).ShouldBe(
        [
            ("Count the ", InlineStyle.Plain), ("trees", InlineStyle.Emphasis), (" with ", InlineStyle.Plain),
            ("x+3", InlineStyle.Code), (" & see ", InlineStyle.Plain), ("about", InlineStyle.Link), (".", InlineStyle.Plain),
        ]);
        p.Inlines[5].Href.ShouldBe("/2020/about");
        blocks[1].ShouldBeOfType<ListBlock>().Items.Count.ShouldBe(2);
        blocks[2].ShouldBeOfType<CodeBlock>().Text.ShouldBe("..##\n#...\n");
    }

    [Fact]
    public void EachArticleIsOnePart()
    {
        var doc = PuzzleParser.Parse(Part1 + Part2)!;

        doc.Parts.Count.ShouldBe(2);
        doc.Parts[1].Blocks.Count.ShouldBe(1);
    }

    [Fact]
    public void SolvedPartsExposeTheirAnswers()
    {
        var html = Part1 + "<p>Your puzzle answer was <code>242</code>.</p>" + Part2 + "<p>Your puzzle answer was <code>2265549792</code>.</p>";

        PuzzleParser.Parse(html)!.Answers.ShouldBe(["242", "2265549792"]);
    }

    [Fact]
    public void UnknownElementsDegradeToTheirText()
    {
        var html = "<article class=\"day-desc\"><h2>--- Day 1: A ---</h2><p>a <span title=\"egg\">b</span> <blink>c</blink></p><details>d</details></article>";

        var blocks = PuzzleParser.Parse(html)!.Parts[0].Blocks;

        blocks[0].ShouldBeOfType<ParagraphBlock>().Inlines.Select(i => i.Text).ShouldBe(["a ", "b", " ", "c"]);
        blocks[1].ShouldBeOfType<ParagraphBlock>().Inlines.Single().Text.ShouldBe("d");
    }

    [Theory]
    [InlineData("")]
    [InlineData("<html>please log in</html>")]
    public void PagesWithoutAnArticleAreAParseFailure(string html)
    {
        PuzzleParser.Parse(html).ShouldBeNull();
    }
}
