using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Diagnostics.Application;
using Microsoft.Extensions.Logging;

namespace AoCEditor;

public partial class DebugPage : UserControl
{
    private const string All = "All";
    private static readonly string[] Levels = [All, nameof(LogLevel.Debug), nameof(LogLevel.Information), nameof(LogLevel.Warning), nameof(LogLevel.Error)];

    private readonly AppServices? _services;
    private bool _refreshQueued;
    private bool _refreshing;

    public DebugPage() : this(null)
    {
    }

    public DebugPage(AppServices? services)
    {
        _services = services;
        InitializeComponent();
        DebugLevel.ItemsSource = Levels;
        DebugLevel.SelectedIndex = 0;
        DebugSource.ItemsSource = new[] { All };
        DebugSource.SelectedIndex = 0;
        Refresh();
        if (services is not null)
        {
            AttachedToVisualTree += async (_, _) =>
            {
                services.Diagnostics.Buffer.Changed += OnSourceChanged;
                services.Diagnostics.Requests.Changed += OnSourceChanged;
                Refresh();
                await LoadStatusAsync();
            };
            DetachedFromVisualTree += (_, _) =>
            {
                services.Diagnostics.Buffer.Changed -= OnSourceChanged;
                services.Diagnostics.Requests.Changed -= OnSourceChanged;
            };
        }
    }

    public IReadOnlyList<string> LogLines { get; private set; } = [];

    public string Preview => DebugPreview.Text ?? string.Empty;

    public bool IsPreviewVisible => DebugPreviewPanel.IsVisible;

    public void SelectLevel(string name) => DebugLevel.SelectedItem = name;

    public void SelectSource(string name)
    {
        DebugSource.ItemsSource = new[] { All, name };
        DebugSource.SelectedItem = name;
    }

    public async Task PreviewDiagnosticsAsync(CancellationToken cancellationToken)
    {
        if (_services is null)
        {
            return;
        }

        var snapshot = await _services.SnapshotAsync(cancellationToken);
        DebugPreview.Text = DiagnosticsReport.Build(snapshot, _services.Diagnostics.Redactor);
        DebugPreviewPanel.IsVisible = true;
        DebugPreview.Focus();
    }

    private void OnSourceChanged(object? sender, EventArgs e)
    {
        if (_refreshQueued)
        {
            return;
        }

        _refreshQueued = true;
        Dispatcher.UIThread.Post(() =>
        {
            _refreshQueued = false;
            Refresh();
        });
    }

    private void Refresh()
    {
        if (_services is null || DebugLevel is null || _refreshing)
        {
            return;
        }

        _refreshing = true;
        try
        {
            Rebuild(_services);
        }
        finally
        {
            _refreshing = false;
        }
    }

    private void Rebuild(AppServices services)
    {

        var entries = services.Diagnostics.Buffer.Snapshot();
        var sources = new[] { All }.Concat(LogFilter.Sources(entries)).ToList();
        var chosenSource = DebugSource.SelectedItem as string;
        if (chosenSource is not null && !sources.Contains(chosenSource))
        {
            sources.Add(chosenSource);
        }

        if (!DebugSource.Items.OfType<string>().SequenceEqual(sources))
        {
            DebugSource.ItemsSource = sources;
            DebugSource.SelectedItem = chosenSource ?? All;
        }

        var level = DebugLevel.SelectedItem is string name && Enum.TryParse<LogLevel>(name, out var parsed) ? parsed : (LogLevel?)null;
        var source = chosenSource is null or All ? null : chosenSource;
        LogLines = [.. LogFilter.Apply(entries, level, source).Select(LogFilter.Line)];
        DebugLog.ItemsSource = LogLines;
        DebugRequests.ItemsSource = services.Diagnostics.Requests.Recent
            .Select(r => $"{r.At:HH:mm:ss} {DiagnosticsReport.RequestLine(r)}")
            .ToList();
    }

    private async Task LoadStatusAsync()
    {
        var snapshot = await _services!.SnapshotAsync(CancellationToken.None);
        DebugVersions.Text = $"App {snapshot.AppVersion}. OS {snapshot.Os}. {snapshot.Runtime}.";
        DebugSessionText.Text = $"Session: {snapshot.SessionState}";
        var storage = snapshot.Storage;
        DebugStorageText.Text = _services.Diagnostics.Redactor.Apply(storage.Error is null
            ? $"Database {storage.DatabasePath}, schema version {storage.SchemaVersion}, {storage.MigrationsApplied} migrations applied."
            : $"Database {storage.DatabasePath} has a problem: {storage.Error}");
    }

    private void OnFilterChanged(object? sender, SelectionChangedEventArgs e) => Refresh();

    private async void OnCopyLogClick(object? sender, RoutedEventArgs e) =>
        await CopyAsync(_services!.Diagnostics.Redactor.Apply(string.Join(Environment.NewLine, LogLines)));

    private async void OnCopyDiagnosticsClick(object? sender, RoutedEventArgs e) => await PreviewDiagnosticsAsync(CancellationToken.None);

    private async void OnConfirmCopyClick(object? sender, RoutedEventArgs e)
    {
        await CopyAsync(Preview);
        DebugPreviewPanel.IsVisible = false;
        DebugCopyDiagnostics.Focus();
    }

    private void OnCancelCopyClick(object? sender, RoutedEventArgs e)
    {
        DebugPreviewPanel.IsVisible = false;
        DebugCopyDiagnostics.Focus();
    }

    private async Task CopyAsync(string text)
    {
        if (TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard)
        {
            await clipboard.SetTextAsync(text);
        }
    }
}
