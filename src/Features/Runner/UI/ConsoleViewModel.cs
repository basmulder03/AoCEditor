using System.Collections.ObjectModel;
using System.Globalization;
using Runner.Application;
using Shared;

namespace Runner.UI;

public sealed record ConsoleLine(string Text, bool IsError);

/// <summary>Output of the latest run plus a one-line status; the line count is capped so a program that prints forever cannot exhaust memory.</summary>
public sealed class ConsoleViewModel(int maxLines = 10_000) : ObservableBase
{
    private string _status = string.Empty;
    private bool _running;
    private bool _truncated;

    public ObservableCollection<ConsoleLine> Lines { get; } = [];

    public string Status { get => _status; private set => Set(ref _status, value); }

    public bool IsRunning { get => _running; private set => Set(ref _running, value); }

    /// <summary>True when lines after the cap were dropped.</summary>
    public bool IsTruncated { get => _truncated; private set => Set(ref _truncated, value); }

    public void Begin()
    {
        Lines.Clear();
        IsTruncated = false;
        IsRunning = true;
        Status = "Running...";
    }

    public void Append(OutputLine line)
    {
        if (Lines.Count >= maxLines)
        {
            IsTruncated = true;
            return;
        }

        Lines.Add(new ConsoleLine(line.Text, line.Stream == OutputChannel.Stderr));
    }

    public void End(RunResult result)
    {
        IsRunning = false;
        var seconds = result.Elapsed.TotalSeconds.ToString("0.00", CultureInfo.InvariantCulture);
        Status = result.Status switch
        {
            RunStatus.Completed when result.ExitCode == 0 => $"Finished with exit code 0 in {seconds} s",
            RunStatus.Completed => $"Failed with exit code {result.ExitCode} in {seconds} s",
            RunStatus.TimedOut => $"Stopped after the time limit ({seconds} s)",
            RunStatus.Cancelled => $"Cancelled after {seconds} s",
            RunStatus.FailedToStart => $"Could not start: {result.Error}",
            _ => $"Not started: {result.Error}",
        };
    }

    /// <summary>For failures that happen before a run starts, such as a missing toolchain.</summary>
    public void Fail(string message)
    {
        IsRunning = false;
        Status = message;
    }
}
