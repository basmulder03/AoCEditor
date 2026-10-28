using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace AoCEditor;

public partial class ShellWindow : Window
{
    private readonly AppServices? _services;

    public ShellWindow() : this(null)
    {
    }

    public ShellWindow(AppServices? services)
    {
        _services = services;
        InitializeComponent();
        Host.Content = new CalendarPage();
        if (services is not null)
        {
            services.Events.SignInRequired += (_, _) => Dispatcher.UIThread.Post(() => SignInPrompt.IsVisible = true);
            services.SignedIn += (_, _) => Dispatcher.UIThread.Post(() => SignInPrompt.IsVisible = false);
            Opened += async (_, _) => await services.InitializeAsync(CancellationToken.None);
        }
    }

    public Control? CurrentPage => Host.Content as Control;

    public bool IsSignInPromptVisible => SignInPrompt.IsVisible;

    public void NavigateTo(string page) =>
        Nav.SelectedItem = Nav.Items.OfType<ListBoxItem>().Single(i => (string?)i.Tag == page);

    void OnNavChanged(object? sender, SelectionChangedEventArgs e)
    {
        // Raised for the initial selection while the XAML is still being built.
        if (Host is null)
        {
            return;
        }

        if (Nav.SelectedItem is ListBoxItem { Tag: "Settings" })
        {
            Host.Content = new SettingsPage(_services);
        }
        else if (Nav.SelectedItem is ListBoxItem)
        {
            Host.Content = new CalendarPage();
        }
    }

    async void OnPromptSignInClick(object? sender, RoutedEventArgs e)
    {
        SignInPrompt.IsVisible = false;
        NavigateTo("Settings");
        await ((SettingsPage)CurrentPage!).BeginSignInAsync();
    }

    void OnPromptDismissClick(object? sender, RoutedEventArgs e) => SignInPrompt.IsVisible = false;
}
