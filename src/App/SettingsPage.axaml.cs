using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Session.Application;
using UiKit;

namespace AoCEditor;

public partial class SettingsPage : UserControl
{
    private readonly AppServices? _services;

    public SettingsPage() : this(null)
    {
    }

    public SettingsPage(AppServices? services)
    {
        _services = services;
        InitializeComponent();
        ThemeBox.ItemsSource = ThemeChoice.Names;
        ThemeBox.SelectedItem = ThemeChoice.Names.FirstOrDefault(n => ThemeChoice.Resolve(n) == Application.Current!.RequestedThemeVariant) ?? "System";
        AccountPanel.IsVisible = services is not null;
        if (services is not null)
        {
            AttachedToVisualTree += async (_, _) => await RefreshAsync();
        }
    }

    public void SelectTheme(string name) => ThemeBox.SelectedItem = name;

    public async Task BeginSignInAsync()
    {
        if (_services is null)
        {
            return;
        }

        SetBusy(true);
        MessageText.Text = "Waiting for the sign-in window...";
        Show(await _services.SignInWithBrowserAsync(CancellationToken.None));
        await RefreshAsync();
    }

    void OnThemeChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ThemeBox?.SelectedItem is string name)
        {
            ThemeChoice.Apply(Application.Current!, name);
        }
    }

    async void OnSignInClick(object? sender, RoutedEventArgs e) => await BeginSignInAsync();

    void OnPasteToggleClick(object? sender, RoutedEventArgs e) => PastePanel.IsVisible = !PastePanel.IsVisible;

    async void OnUseTokenClick(object? sender, RoutedEventArgs e)
    {
        SetBusy(true);
        var result = await _services!.SignInWithPastedTokenAsync(PasteBox.Text ?? string.Empty, CancellationToken.None);
        PasteBox.Text = string.Empty;
        Show(result);
        await RefreshAsync();
    }

    async void OnSignOutClick(object? sender, RoutedEventArgs e)
    {
        SetBusy(true);
        MessageText.Text = await _services!.SignOutAsync(CancellationToken.None) is SecretResult.Unavailable u
            ? $"Secure storage is unavailable: {u.Reason}"
            : "Signed out.";
        await RefreshAsync();
    }

    async Task RefreshAsync()
    {
        var status = await _services!.GetStatusAsync(CancellationToken.None);
        StatusText.Text = status.State switch
        {
            SessionState.SignedIn => "Signed in.",
            SessionState.StoreUnavailable => $"Secure storage is unavailable ({status.Reason}). Signing in needs a Secret Service such as GNOME Keyring or KWallet on Linux, or Credential Manager on Windows.",
            _ => "Not signed in.",
        };
        var usable = status.State != SessionState.StoreUnavailable;
        SignInButton.IsEnabled = usable && status.State == SessionState.SignedOut;
        PasteToggle.IsEnabled = usable;
        SignOutButton.IsEnabled = usable && status.State == SessionState.SignedIn;
        UseTokenButton.IsEnabled = usable;
        if (!usable)
        {
            PastePanel.IsVisible = false;
        }
    }

    void SetBusy(bool busy)
    {
        SignInButton.IsEnabled = !busy;
        SignOutButton.IsEnabled = !busy;
        UseTokenButton.IsEnabled = !busy;
    }

    void Show(SignInResult result)
    {
        MessageText.Text = result switch
        {
            SignInResult.SignedIn => "Signed in.",
            SignInResult.Cancelled => "Sign-in was cancelled.",
            SignInResult.InvalidToken => "Advent of Code did not accept that token. Check that you copied the whole session value.",
            SignInResult.StoreUnavailable u => $"Secure storage is unavailable: {u.Reason}",
            SignInResult.NetworkError => "Could not reach Advent of Code. Try again in a moment.",
            SignInResult.BrowserUnsupported => "The sign-in window is not available on this system. Paste your session token instead.",
            _ => string.Empty,
        };
        if (result is SignInResult.BrowserUnsupported)
        {
            PastePanel.IsVisible = true;
            PasteBox.Focus();
        }
    }
}
