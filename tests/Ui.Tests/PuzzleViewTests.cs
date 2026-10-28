using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.VisualTree;
using Puzzle.Domain;
using Puzzle.UI;
using Shouldly;

namespace Ui.Tests;

public class PuzzleViewTests
{
    private static readonly PuzzlePart Part = new(
    [
        new ParagraphBlock([new Fragment("Count "), new Fragment("trees", InlineStyle.Emphasis), new Fragment(" at "), new Fragment("x+3", InlineStyle.Code)]),
        new ListBlock([[new Fragment("one")], [new Fragment("two")]]),
        new CodeBlock("..#\n#..\n"),
    ]);

    private static PuzzleView Show()
    {
        var view = new PuzzleView { Part = Part };
        new Window { Content = view, Width = 600, Height = 400 }.Show();
        return view;
    }

    [AvaloniaFact]
    public void BlocksBecomeSelectableText()
    {
        var texts = Show().GetVisualDescendants().OfType<SelectableTextBlock>().ToList();

        texts.Select(t => t.Text ?? string.Concat(t.Inlines!.OfType<Run>().Select(r => r.Text))).ShouldBe(
            ["Count trees at x+3", "• one", "• two", "..#\n#..\n"]);
    }

    [AvaloniaFact]
    public void CodeIsMonospaceAndEmphasisIsItalic()
    {
        var runs = Show().GetVisualDescendants().OfType<SelectableTextBlock>().First().Inlines!.OfType<Run>().ToList();

        runs[1].FontStyle.ShouldBe(FontStyle.Italic);
        runs[3].FontFamily.Name.ShouldContain("Mono");
    }

    [AvaloniaFact]
    public void ReplacingThePartReplacesTheContent()
    {
        var view = Show();

        view.Part = new PuzzlePart([new CodeBlock("x")]);

        view.GetVisualDescendants().OfType<SelectableTextBlock>().Single().Text.ShouldBe("x");
    }
}
