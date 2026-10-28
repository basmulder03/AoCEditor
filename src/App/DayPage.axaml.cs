using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AoCEditor;

public partial class DayPage : UserControl
{
    private readonly Action _back;

    public DayPage() : this(0, 0, () => { })
    {
    }

    public DayPage(int year, int day, Action back)
    {
        _back = back;
        InitializeComponent();
        Title.Text = $"Day {day} of {year} - coming soon";
        Year = year;
        Day = day;
    }

    public int Year { get; }

    public int Day { get; }

    void OnBackClick(object? sender, RoutedEventArgs e) => _back();
}
