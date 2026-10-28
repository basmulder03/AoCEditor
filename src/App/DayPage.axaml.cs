using Avalonia.Controls;
using Avalonia.Interactivity;
using Gateway;
using Puzzle.UI;

namespace AoCEditor;

public partial class DayPage : UserControl
{
    private readonly Action _back;

    public DayPage() : this(null, 0, 0, () => { })
    {
    }

    public DayPage(AppServices? services, int year, int day, Action back)
    {
        _back = back;
        InitializeComponent();
        Year = year;
        Day = day;
        if (services is null)
        {
            Title.IsVisible = true;
            Title.Text = $"Day {day} of {year}";
            return;
        }

        var model = new DayViewModel(services.Puzzle, year, day, TimeProvider.System, new GatewayOptions().BaseAddress);
        View.DataContext = model;
        AttachedToVisualTree += async (_, _) => await model.LoadAsync(false, CancellationToken.None);
    }

    public int Year { get; }

    public int Day { get; }

    void OnBackClick(object? sender, RoutedEventArgs e) => _back();
}
