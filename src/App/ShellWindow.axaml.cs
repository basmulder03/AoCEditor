using Avalonia.Controls;

namespace AoCEditor;

public partial class ShellWindow : Window
{
    public ShellWindow()
    {
        InitializeComponent();
        Host.Content = new CalendarPage();
    }

    public Control? CurrentPage => Host.Content as Control;

    public void NavigateTo(string page) =>
        Nav.SelectedItem = Nav.Items.OfType<ListBoxItem>().Single(i => (string?)i.Tag == page);

    void OnNavChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (Nav.SelectedItem is ListBoxItem { Tag: "Settings" })
        {
            Host.Content = new SettingsPage();
        }
        else if (Nav.SelectedItem is ListBoxItem)
        {
            Host.Content = new CalendarPage();
        }
    }
}
