using AoCEditor.Core.Session.Application;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
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
        if (OperatingSystem.IsLinux())
        {
            SignInButton.Content = "Try browser sign-in (experimental)";
        }

        DebugModeBox.IsVisible = services is not null;
        if (services is not null)
        {
            DebugModeBox.IsChecked = services.Diagnostics.Debug.IsEnabled;
            DebugModeBox.IsEnabled = !services.Diagnostics.Debug.IsForced;
            DebugModeNote.IsVisible = services.Diagnostics.Debug.IsForced;
        }

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

    async void OnDebugModeChanged(object? sender, RoutedEventArgs e)
    {
        if (_services is not null && DebugModeBox.IsChecked is { } on && on != _services.Diagnostics.Debug.IsEnabled)
        {
            await _services.Diagnostics.Debug.SetAsync(on, CancellationToken.None);
        }
    }

    async void OnSignInClick(object? sender, RoutedEventArgs e) => await BeginSignInAsync();

    public void FocusAccount()
    {
        AccountPanel.BringIntoView();
        PasteBox.Focus();
    }

    void OnRevealChanged(object? sender, RoutedEventArgs e) => PasteBox.PasswordChar = RevealToggle.IsChecked == true ? '\0' : '*';

    async void OnOpenSiteClick(object? sender, RoutedEventArgs e)
    {
        var launcher = TopLevel.GetTopLevel(this)?.Launcher;
        if (launcher is null || !await launcher.LaunchUriAsync(_services!.SignInUri))
        {
            MessageText.Text = $"Could not open your browser. Open {_services!.SignInUri} yourself.";
        }
    }

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
        var signedOut = status.State == SessionState.SignedOut;
        SignOutButton.IsVisible = status.State == SessionState.SignedIn;
        SignOutButton.IsEnabled = true;
        PastePanel.IsVisible = status.State != SessionState.SignedIn;
        SignInButton.IsEnabled = usable && signedOut;
        UseTokenButton.IsEnabled = usable;
        OpenSiteButton.IsEnabled = true;
    }

    void SetBusy(bool busy)
    {
        SignInButton.IsEnabled = !busy;
        SignOutButton.IsEnabled = !busy;
        UseTokenButton.IsEnabled = !busy;
    }

    void Show(SignInResult result)
    {
        MessageText.Text = SignInResultText.For(result);
        PasteBox.Focus();
    }
}
