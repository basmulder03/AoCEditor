using System.ComponentModel;
using System.Globalization;
using AoCEditor.Core.Puzzle.Domain;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace AoCEditor.UI.Puzzle;

public partial class DayView : UserControl
{
    private readonly DispatcherTimer _tick = new() { Interval = TimeSpan.FromSeconds(1) };
    private DayViewModel? _model;
    private bool _inputRequested;

    public DayView()
    {
        InitializeComponent();
        _tick.Tick += (_, _) => _model?.Tick();
    }

    private DayViewModel? Model => DataContext as DayViewModel;

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (_model is not null)
        {
            _model.PropertyChanged -= OnModelChanged;
        }

        _model = Model;
        if (_model is not null)
        {
            _model.PropertyChanged += OnModelChanged;
            RebuildTabs();
            ShowCooldown();
        }
    }

    protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _tick.Start();
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _tick.Stop();
    }

    void OnModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(DayViewModel.Parts):
            case nameof(DayViewModel.InputText):
                RebuildTabs();
                break;
            case nameof(DayViewModel.CooldownSeconds):
                ShowCooldown();
                break;
        }
    }

    void RebuildTabs()
    {
        if (Model is not { } model)
        {
            return;
        }

        var selected = Tabs.SelectedIndex;
        var tabs = model.Parts
            .Select((part, i) => new TabItem { Header = $"Part {i + 1}", Content = new ScrollViewer { Content = PuzzleViewFor(part) } })
            .ToList();
        tabs.Add(new TabItem
        {
            Header = "Input",
            Content = new ScrollViewer
            {
                Content = new SelectableTextBlock
                {
                    Text = model.InputText ?? "The input is downloaded the first time you open this tab.",
                    FontFamily = new Avalonia.Media.FontFamily("Cascadia Mono, Consolas, DejaVu Sans Mono, monospace"),
                },
            },
        });
        Tabs.ItemsSource = tabs;
        Tabs.SelectedIndex = Math.Clamp(selected, 0, tabs.Count - 1);
    }

    PuzzleView PuzzleViewFor(PuzzlePart part)
    {
        var view = new PuzzleView { Part = part, BaseAddress = Model?.SiteAddress };
        view.LinkRequested += async (_, uri) => await OpenLinkAsync(uri);
        return view;
    }

    async Task OpenLinkAsync(Uri uri)
    {
        if (TopLevel.GetTopLevel(this) is Window window && await ConfirmDialog.AskAsync(window, $"Open {uri.AbsoluteUri} in your browser?"))
        {
            await window.Launcher.LaunchUriAsync(uri);
        }
    }

    void ShowCooldown() =>
        CooldownText.Text = Model is { CooldownSeconds: > 0 and var s }
            ? string.Create(CultureInfo.InvariantCulture, $"You can answer again in {s}s")
            : string.Empty;

    async void OnTabChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (Model is { } model && !_inputRequested && Tabs.SelectedItem is TabItem { Header: "Input" })
        {
            _inputRequested = true;
            await model.LoadInputAsync(false, CancellationToken.None);
        }
    }

    async void OnSubmitClick(object? sender, RoutedEventArgs e)
    {
        if (Model is { } model)
        {
            await model.SubmitAsync(CancellationToken.None);
        }
    }

    async void OnAnswerKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && Model is { } model)
        {
            await model.SubmitAsync(CancellationToken.None);
        }
    }
}
