using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Puzzle.Domain;

namespace Puzzle.UI;

/// <summary>Renders one puzzle part with native text controls; web links raise <see cref="LinkRequested"/>, other links are shown as underlined text.</summary>
public sealed class PuzzleView : UserControl
{
    private static readonly FontFamily Mono = new("Cascadia Mono, Consolas, DejaVu Sans Mono, monospace");

    public static readonly StyledProperty<PuzzlePart?> PartProperty = AvaloniaProperty.Register<PuzzleView, PuzzlePart?>(nameof(Part));

    public static readonly StyledProperty<Uri?> BaseAddressProperty = AvaloniaProperty.Register<PuzzleView, Uri?>(nameof(BaseAddress));

    /// <summary>The site that relative puzzle links are resolved against.</summary>
    public Uri? BaseAddress
    {
        get => GetValue(BaseAddressProperty);
        set => SetValue(BaseAddressProperty, value);
    }

    public PuzzlePart? Part
    {
        get => GetValue(PartProperty);
        set => SetValue(PartProperty, value);
    }

    /// <summary>Raised with the resolved address when the user clicks a web link; the host decides whether to open it.</summary>
    public event EventHandler<Uri>? LinkRequested;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == PartProperty)
        {
            Content = Part is null ? null : Build(Part);
        }
    }

    private StackPanel Build(PuzzlePart part)
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

    private SelectableTextBlock Text(IEnumerable<Fragment> inlines)
    {
        var text = new SelectableTextBlock { TextWrapping = TextWrapping.Wrap };
        foreach (var inline in inlines)
        {
            if (inline.Style == InlineStyle.Link && LinkResolver.Resolve(BaseAddress, inline.Href) is { } uri)
            {
                var link = new HyperlinkButton { Content = inline.Text, Padding = new Thickness(0) };
                link.Click += (_, _) => LinkRequested?.Invoke(this, uri);
                text.Inlines!.Add(new InlineUIContainer(link));
                continue;
            }

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
