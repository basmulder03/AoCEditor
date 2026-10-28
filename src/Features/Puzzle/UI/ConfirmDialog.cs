using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace Puzzle.UI;

internal static class ConfirmDialog
{
    public static async Task<bool> AskAsync(Window owner, string message)
    {
        var dialog = new Window
        {
            Title = "Open link",
            SizeToContent = SizeToContent.WidthAndHeight,
            MaxWidth = 520,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
        };
        var open = new Button { Content = "Open", IsDefault = true };
        var cancel = new Button { Content = "Cancel", IsCancel = true };
        open.Click += (_, _) => dialog.Close(true);
        cancel.Click += (_, _) => dialog.Close(false);
        dialog.Content = new StackPanel
        {
            Margin = new Thickness(16),
            Spacing = 12,
            Children =
            {
                new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right, Children = { cancel, open } },
            },
        };
        return await dialog.ShowDialog<bool>(owner);
    }
}
