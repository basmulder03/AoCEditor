using AoCEditor.Core.Puzzle.Domain;
using AoCEditor.UI.Puzzle;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using Shouldly;

namespace AoCEditor.UI.Tests;

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

    [AvaloniaFact]
    public void ClickingAWebLinkRequestsItsResolvedAddress()
    {
        var view = new PuzzleView { BaseAddress = new Uri("https://adventofcode.com/"), Part = new PuzzlePart([new ParagraphBlock([new Fragment("see "), new Fragment("about", InlineStyle.Link, "/2020/about")])]) };
        new Window { Content = view, Width = 600, Height = 400 }.Show();
        Uri? requested = null;
        view.LinkRequested += (_, uri) => requested = uri;

        var link = view.GetVisualDescendants().OfType<HyperlinkButton>().Single();
        link.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        requested!.AbsoluteUri.ShouldBe("https://adventofcode.com/2020/about");
    }

    [AvaloniaFact]
    public void UnsafeLinksAreShownButNotClickable()
    {
        var view = new PuzzleView { Part = new PuzzlePart([new ParagraphBlock([new Fragment("x", InlineStyle.Link, "javascript:alert(1)")])]) };
        new Window { Content = view, Width = 600, Height = 400 }.Show();

        view.GetVisualDescendants().OfType<HyperlinkButton>().ShouldBeEmpty();
        view.GetVisualDescendants().OfType<SelectableTextBlock>().Single().Inlines!.OfType<Run>().Single().Text.ShouldBe("x");
    }
}
