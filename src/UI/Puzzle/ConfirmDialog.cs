using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace AoCEditor.UI.Puzzle;

public static class ConfirmDialog
{
    public static async Task<bool> AskAsync(Window owner, string title, string message, string confirmLabel)
    {
        var dialog = new Window
        {
            Title = title,
            SizeToContent = SizeToContent.WidthAndHeight,
            MaxWidth = 520,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
        };
        var confirm = new Button { Content = confirmLabel, IsDefault = true };
        var cancel = new Button { Content = "Cancel", IsCancel = true };
        confirm.Click += (_, _) => dialog.Close(true);
        cancel.Click += (_, _) => dialog.Close(false);
        dialog.Content = new StackPanel
        {
            Margin = new Thickness(16),
            Spacing = 12,
            Children =
            {
                new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right, Children = { cancel, confirm } },
            },
        };
        return await dialog.ShowDialog<bool>(owner);
    }
}
