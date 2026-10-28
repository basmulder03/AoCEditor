using AoCEditor.Core.Plugins.Domain;
using AoCEditor.Core.Runner.Application;
using AoCEditor.Core.Workspace.Application;
using AoCEditor.UI.Editor;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace AoCEditor;

public partial class SolutionPanel : UserControl
{
    private readonly AppServices? _services;
    private int _year;
    private int _day;
    private Window? _window;

    public SolutionPanel() => InitializeComponent();

    public SolutionPanel(AppServices services, int year, int day) : this()
    {
        _services = services;
        _year = year;
        _day = day;
        var model = new SolutionViewModel(
            services.Plugins,
            (plugin, allow, output, cancellationToken) => SaveThenRunAsync(year, day, plugin, allow, output, cancellationToken),
            ConfirmUnsandboxedAsync,
            action => Dispatcher.UIThread.Post(action));
        DataContext = model;
        AttachedToVisualTree += async (_, _) =>
        {
            if (TopLevel.GetTopLevel(this) is Window window)
            {
                _window = window;
                window.Activated += OnWindowActivated;
            }

            await ShowFolderAsync();
            await OpenSourceAsync();
        };
        DetachedFromVisualTree += (_, _) =>
        {
            if (_window is not null)
            {
                _window.Activated -= OnWindowActivated;
                _window = null;
            }
        };
    }

    public EditorViewModel? Editor { get; private set; }

    void OnWindowActivated(object? sender, EventArgs e) => Editor?.CheckDisk();

    // The file the user is editing must be on disk before it runs; a conflicting disk change is left to the editor's banner.
    async Task<DayRunResult> SaveThenRunAsync(int year, int day, PluginManifest plugin, bool allow, IProgress<OutputLine> output, CancellationToken cancellationToken)
    {
        if (Editor is { IsDirty: true } editor && editor.Save() == SaveResult.ChangedOnDisk)
        {
            return new DayRunResult(DayRunKind.SourceChanged, Detail: "The file changed on disk. Reload it or save anyway in the editor, then run again.");
        }

        return await _services!.Coordinator.RunAsync(year, day, plugin, output, allow, cancellationToken);
    }

    bool CanLeaveFile() => Editor is not { IsDirty: true } editor || editor.Save() != SaveResult.ChangedOnDisk;

    async Task OpenSourceAsync()
    {
        if (_services is null || Model?.SelectedPlugin is not { } plugin)
        {
            return;
        }

        var scaffold = await _services.Coordinator.ScaffoldAsync(_year, _day, plugin, CancellationToken.None);
        if (scaffold is { Kind: DayScaffoldKind.Scaffolded, Folder: { } folder, SourcePath: { } source })
        {
            var editor = new EditorViewModel(folder);
            editor.Load(source);
            Editor = editor;
            CodeEditor.DataContext = editor;
        }
    }

    private SolutionViewModel? Model => DataContext as SolutionViewModel;

    async Task<bool> ConfirmUnsandboxedAsync(string detail) =>
        TopLevel.GetTopLevel(this) is Window window
        && await ConfirmDialog.AskAsync(
            window,
            "Run with an incomplete sandbox?",
            $"{detail} Without these protections your solution runs with your normal user rights where they are missing and could read or change your files or use the network. Only run code you trust.",
            "Run anyway");

    async Task ShowFolderAsync() =>
        FolderText.Text = _services is null ? string.Empty : await _services.Workspace.RootAsync(CancellationToken.None) ?? "No solutions folder chosen yet.";

    void OnLanguageChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (Model is { } model && LanguageBox.SelectedIndex is >= 0 and var i && _services is { } services)
        {
            if (services.Plugins[i] == model.SelectedPlugin)
            {
                return;
            }

            if (!CanLeaveFile())
            {
                LanguageBox.SelectedIndex = services.Plugins.ToList().IndexOf(model.SelectedPlugin!);
                return;
            }

            model.SelectedPlugin = services.Plugins[i];
            _ = OpenSourceAsync();
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

        await ApplyFolderAsync(path);
    }

    public async Task ApplyFolderAsync(string path)
    {
        if (!CanLeaveFile())
        {
            return;
        }

        var result = await _services!.Workspace.SetRootAsync(path, CancellationToken.None);
        if (result == SetRootResult.Ok)
        {
            await OpenSourceAsync();
        }

        FolderText.Text = result switch
        {
            SetRootResult.Ok => path,
            SetRootResult.OverlapsAppData => "That folder overlaps the app's data folder. Choose another one, so inputs never end up in your solutions.",
            _ => "Choose a full path.",
        };
    }
}
