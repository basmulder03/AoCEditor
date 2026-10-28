using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace AoCEditor;

public partial class ShellWindow : Window
{
    private readonly AppServices? _services;
    private readonly CalendarPage _calendar;

    public ShellWindow() : this(null)
    {
    }

    public ShellWindow(AppServices? services)
    {
        _services = services;
        InitializeComponent();
        _calendar = new CalendarPage(services);
        _calendar.DaySelected += (_, e) => Host.Content = new DayPage(services, e.Year, e.Day, () => Host.Content = _calendar);
        _calendar.AccountRequested += (_, _) => OnPromptSignInClick(null, null);
        Host.Content = _calendar;
        if (services is not null)
        {
            services.Events.SignInRequired += (_, _) => Dispatcher.UIThread.Post(() => SignInPrompt.IsVisible = true);
            services.SignedIn += (_, _) => Dispatcher.UIThread.Post(() => SignInPrompt.IsVisible = false);
            services.Diagnostics.Debug.Changed += (_, _) => Dispatcher.UIThread.Post(SyncDebugEntry);
            SyncDebugEntry();
            Opened += async (_, _) => await services.InitializeAsync(CancellationToken.None);
        }
    }

    public Control? CurrentPage => Host.Content as Control;

    public bool IsSignInPromptVisible => SignInPrompt.IsVisible;

    public bool HasNavigationEntry(string page) => Nav.Items.OfType<ListBoxItem>().Any(i => (string?)i.Tag == page);

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
        else if (Nav.SelectedItem is ListBoxItem { Tag: "Debug" })
        {
            Host.Content = new DebugPage(_services);
        }
        else if (Nav.SelectedItem is ListBoxItem)
        {
            Host.Content = _calendar;
        }
    }

    // The Debug entry exists only while debug mode is on; leaving it shows the calendar.
    void SyncDebugEntry()
    {
        var enabled = _services!.Diagnostics.Debug.IsEnabled;
        var entry = Nav.Items.OfType<ListBoxItem>().FirstOrDefault(i => (string?)i.Tag == "Debug");
        if (enabled && entry is null)
        {
            var item = new ListBoxItem { Tag = "Debug", Content = "Debug" };
            Avalonia.Automation.AutomationProperties.SetName(item, "Debug");
            Nav.Items.Add(item);
        }
        else if (!enabled && entry is not null)
        {
            if (Nav.SelectedItem == entry)
            {
                Nav.SelectedIndex = 0;
            }

            Nav.Items.Remove(entry);
        }
    }

    void OnPromptSignInClick(object? sender, RoutedEventArgs? e)
    {
        SignInPrompt.IsVisible = false;
        NavigateTo("Settings");
        ((SettingsPage)CurrentPage!).FocusAccount();
    }

    void OnPromptDismissClick(object? sender, RoutedEventArgs e) => SignInPrompt.IsVisible = false;
}
