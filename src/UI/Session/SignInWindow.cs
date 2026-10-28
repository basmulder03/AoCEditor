using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace AoCEditor.UI.Session;

/// <summary>Our own window around the embedded browser: movable, resizable, with a status line and an always-visible Cancel.</summary>
public sealed class SignInWindow : Window
{
    private const int DefaultWidth = 520;
    private const int DefaultHeight = 720;
    private readonly TextBlock _status = new() { TextWrapping = Avalonia.Media.TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center };

    public SignInWindow()
    {
        Title = "Sign in to Advent of Code";
        MinWidth = 360;
        MinHeight = 480;
        Width = DefaultWidth;
        Height = DefaultHeight;
        CanResize = true;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        var cancel = new Button { Content = "Cancel", HorizontalAlignment = HorizontalAlignment.Right };
        cancel.Click += (_, _) => Close();
        Avalonia.Automation.AutomationProperties.SetName(cancel, "Cancel sign-in");
        Browser.HorizontalAlignment = HorizontalAlignment.Stretch;
        Browser.VerticalAlignment = VerticalAlignment.Stretch;

        var bar = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Margin = new Thickness(8) };
        bar.Children.Add(_status);
        Grid.SetColumn(cancel, 1);
        bar.Children.Add(cancel);

        var root = new Grid { RowDefinitions = new RowDefinitions("*,Auto") };
        root.Children.Add(Browser);
        Grid.SetRow(bar, 1);
        root.Children.Add(bar);
        Content = root;
    }

    public NativeWebView Browser { get; } = new();

    public string Status
    {
        get => _status.Text ?? string.Empty;
        set => _status.Text = value;
    }

    /// <summary>Centers on the owner and keeps the window on one screen; falls back to the screen center without an owner.</summary>
    public void PlaceOver(Window? owner)
    {
        if (owner is null || owner.Screens.All.Count == 0)
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            return;
        }

        var scale = owner.Screens.ScreenFromWindow(owner)?.Scaling ?? owner.RenderScaling;
        var ownerBounds = new PixelRect(owner.Position, PixelSize.FromSizeWithDpi(owner.Bounds.Size, 96 * owner.RenderScaling));
        var placed = SignInWindowPlacement.Place(
            ownerBounds,
            owner.Screens.All.Select(s => s.WorkingArea).ToList(),
            new PixelSize((int)(DefaultWidth * scale), (int)(DefaultHeight * scale)));
        WindowStartupLocation = WindowStartupLocation.Manual;
        Position = placed.Position;
        Width = placed.Width / scale;
        Height = placed.Height / scale;
    }
}
