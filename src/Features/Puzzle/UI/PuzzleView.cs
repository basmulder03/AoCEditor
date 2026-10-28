using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Puzzle.Domain;

namespace Puzzle.UI;

/// <summary>Renders one puzzle part with native text controls; links are shown but not yet clickable.</summary>
public sealed class PuzzleView : UserControl
{
    private static readonly FontFamily Mono = new("Cascadia Mono, Consolas, DejaVu Sans Mono, monospace");

    public static readonly StyledProperty<PuzzlePart?> PartProperty = AvaloniaProperty.Register<PuzzleView, PuzzlePart?>(nameof(Part));

    public PuzzlePart? Part
    {
        get => GetValue(PartProperty);
        set => SetValue(PartProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == PartProperty)
        {
            Content = Part is null ? null : Build(Part);
        }
    }

    private static StackPanel Build(PuzzlePart part)
    {
        var panel = new StackPanel { Spacing = 10 };
        foreach (var block in part.Blocks)
        {
            switch (block)
            {
                case ParagraphBlock p:
                    panel.Children.Add(Text(p.Inlines));
                    break;
                case ListBlock l:
                    panel.Children.AddRange(l.Items.Select(item => Text([new Fragment("• "), .. item])));
                    break;
                case CodeBlock c:
                    panel.Children.Add(new Border
                    {
                        Padding = new Thickness(8),
                        CornerRadius = new CornerRadius(4),
                        Background = new SolidColorBrush(Colors.Gray, 0.15),
                        Child = new SelectableTextBlock { Text = c.Text, FontFamily = Mono },
                    });
                    break;
            }
        }

        return panel;
    }

    private static SelectableTextBlock Text(IEnumerable<Fragment> inlines)
    {
        var text = new SelectableTextBlock { TextWrapping = TextWrapping.Wrap };
        foreach (var inline in inlines)
        {
            var run = new Run(inline.Text);
            switch (inline.Style)
            {
                case InlineStyle.Emphasis:
                    run.FontStyle = FontStyle.Italic;
                    break;
                case InlineStyle.Code:
                    run.FontFamily = Mono;
                    break;
                case InlineStyle.Link:
                    run.TextDecorations = TextDecorations.Underline;
                    break;
            }

            text.Inlines!.Add(run);
        }

        return text;
    }
}
