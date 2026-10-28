using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Workspace.Application;

namespace AoCEditor;

public partial class SolutionPanel : UserControl
{
    private readonly AppServices? _services;

    public SolutionPanel() => InitializeComponent();

    public SolutionPanel(AppServices services, int year, int day) : this()
    {
        _services = services;
        var model = new SolutionViewModel(
            services.Plugins,
            (plugin, allow, output, ct) => services.Coordinator.RunAsync(year, day, plugin, output, allow, ct),
            ConfirmUnsandboxedAsync,
            action => Dispatcher.UIThread.Post(action));
        DataContext = model;
        AttachedToVisualTree += async (_, _) => await ShowFolderAsync();
    }

    private SolutionViewModel? Model => DataContext as SolutionViewModel;

    async Task<bool> ConfirmUnsandboxedAsync() =>
        TopLevel.GetTopLevel(this) is Window window
        && await ConfirmDialog.AskAsync(
            window,
            "Run without a sandbox?",
            "This version cannot isolate your solution yet. It will run with your normal user rights and could read or change your files. Only run code you trust.",
            "Run anyway");

    async Task ShowFolderAsync() =>
        FolderText.Text = _services is null ? string.Empty : await _services.Workspace.RootAsync(CancellationToken.None) ?? "No solutions folder chosen yet.";

    void OnLanguageChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (Model is { } model && LanguageBox.SelectedIndex is >= 0 and var i && _services is { } services)
        {
            model.SelectedPlugin = services.Plugins[i];
        }
    }

    async void OnRunClick(object? sender, RoutedEventArgs e)
    {
        if (Model is { } model)
        {
            await model.RunAsync(CancellationToken.None);
        }
    }

    void OnStopClick(object? sender, RoutedEventArgs e) => Model?.Stop();

    async void OnChooseFolderClick(object? sender, RoutedEventArgs e)
    {
        if (_services is null || TopLevel.GetTopLevel(this) is not { } top)
        {
            return;
        }

        var picked = await top.StorageProvider.OpenFolderPickerAsync(new Avalonia.Platform.Storage.FolderPickerOpenOptions { Title = "Choose your solutions folder", AllowMultiple = false });
        if (picked.Count == 0 || picked[0].Path is not { IsFile: true } uri || uri.LocalPath is not { } path)
        {
            return;
        }

        var result = await _services.Workspace.SetRootAsync(path, CancellationToken.None);
        FolderText.Text = result switch
        {
            SetRootResult.Ok => path,
            SetRootResult.OverlapsAppData => "That folder overlaps the app's data folder. Choose another one, so inputs never end up in your solutions.",
            _ => "Choose a full path.",
        };
    }
}
