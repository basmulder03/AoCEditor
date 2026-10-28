using Plugins.Domain;
using Runner.Application;
using Runner.UI;
using Shared;

namespace AoCEditor;

/// <summary>The solution side of the day page: pick a language, run, stop and read the console.</summary>
public sealed class SolutionViewModel : ObservableBase
{
    private readonly IReadOnlyList<PluginManifest> _plugins;
    private readonly Func<PluginManifest, bool, IProgress<OutputLine>, CancellationToken, Task<DayRunResult>> _run;
    private readonly Func<Task<bool>> _confirmUnsandboxed;
    private readonly Action<Action> _post;
    private PluginManifest? _selected;
    private bool _running;
    private CancellationTokenSource? _cancel;

    /// <param name="post">Runs an action on the UI thread; output arrives from other threads.</param>
    public SolutionViewModel(
        IReadOnlyList<PluginManifest> plugins,
        Func<PluginManifest, bool, IProgress<OutputLine>, CancellationToken, Task<DayRunResult>> run,
        Func<Task<bool>> confirmUnsandboxed,
        Action<Action> post)
    {
        _plugins = plugins;
        _run = run;
        _confirmUnsandboxed = confirmUnsandboxed;
        _post = post;
        _selected = plugins.Count > 0 ? plugins[0] : null;
    }

    public ConsoleViewModel Console { get; } = new();

    public IReadOnlyList<string> PluginNames => [.. _plugins.Select(p => p.Name)];

    public PluginManifest? SelectedPlugin
    {
        get => _selected;
        set
        {
            Set(ref _selected, value);
            Raise(nameof(CanRun));
        }
    }

    public bool IsRunning { get => _running; private set { Set(ref _running, value); Raise(nameof(CanRun)); } }

    public bool CanRun => !IsRunning && SelectedPlugin is not null;

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        if (!CanRun)
        {
            return;
        }

        IsRunning = true;
        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _cancel = cancel;
        try
        {
            var result = await RunOnceAsync(allowUnsandboxed: false, cancel.Token);
            if (result.Kind == DayRunKind.SandboxRefused)
            {
                if (await _confirmUnsandboxed())
                {
                    result = await RunOnceAsync(allowUnsandboxed: true, cancel.Token);
                }
                else
                {
                    Console.Fail("The solution was not run because no sandbox is available.");
                    return;
                }
            }

            Show(result);
        }
        catch (OperationCanceledException)
        {
            Console.Fail("Cancelled.");
        }
        finally
        {
            _cancel = null;
            IsRunning = false;
        }
    }

    public void Stop() => _cancel?.Cancel();

    private Task<DayRunResult> RunOnceAsync(bool allowUnsandboxed, CancellationToken cancellationToken)
    {
        Console.Begin();
        return _run(SelectedPlugin!, allowUnsandboxed, new Sink(line => _post(() => Console.Append(line))), cancellationToken);
    }

    private void Show(DayRunResult result)
    {
        if (result.Kind == DayRunKind.Ran && result.Run is { } run)
        {
            Console.End(run);
        }
        else
        {
            Console.Fail(result.Detail ?? "The run did not start.");
        }
    }

    private sealed class Sink(Action<OutputLine> report) : IProgress<OutputLine>
    {
        public void Report(OutputLine value) => report(value);
    }
}
