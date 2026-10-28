using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace AoCEditor.UI.Sync;

public partial class CalendarView : UserControl
{
    private const int Columns = 5;

    public CalendarView() => InitializeComponent();

    private CalendarViewModel? Model => DataContext as CalendarViewModel;

    /// <summary>Tiles in reading order, locked ones included.</summary>
    public IReadOnlyList<Button> Tiles => [.. DayGrid.GetVisualDescendants().OfType<Button>().Where(b => b.Classes.Contains("tile"))];

    async void OnYearChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (Model is { } model && YearBox.SelectedItem is int year)
        {
            await model.SelectYearAsync(year, CancellationToken.None);
        }
    }

    void OnTileClick(object? sender, RoutedEventArgs e)
    {
        if (Model is { } model && (sender as Control)?.DataContext is DayTile tile)
        {
            model.Select(tile);
        }
    }

    void OnGridKeyDown(object? sender, KeyEventArgs e)
    {
        var step = e.Key switch
        {
            Key.Left => -1,
            Key.Right => 1,
            Key.Up => -Columns,
            Key.Down => Columns,
            _ => 0,
        };
        if (step == 0)
        {
            return;
        }

        var tiles = Tiles;
        var current = tiles.ToList().FindIndex(t => t.IsFocused);
        var next = current + step;
        if (current >= 0 && next >= 0 && next < tiles.Count)
        {
            tiles[next].Focus();
        }

        e.Handled = true;
    }

    async void OnSyncYearClick(object? sender, RoutedEventArgs e)
    {
        if (Model is { } model)
        {
            await model.SyncYearAsync();
        }
    }

    async void OnSyncAllClick(object? sender, RoutedEventArgs e)
    {
        if (Model is { } model)
        {
            await model.SyncAllAsync();
        }
    }

    void OnCancelClick(object? sender, RoutedEventArgs e) => Model?.Cancel();

    void OnAccountClick(object? sender, RoutedEventArgs e) => Model?.RequestAccount();
}
