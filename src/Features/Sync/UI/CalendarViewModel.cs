using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Contracts;
using Sync.Application;
using Sync.Domain;

namespace Sync.UI;

/// <summary>State of the year calendar: the year list, the day tiles of the selected year and the user-triggered sync.</summary>
public sealed class CalendarViewModel : ObservableBase, IDisposable
{
    private static readonly TimeSpan Tick = TimeSpan.FromSeconds(1);

    private readonly SyncService _sync;
    private readonly ISyncStateStore _store;
    private readonly TimeProvider _time;
    private readonly ILogger _log;
    private readonly SynchronizationContext? _context = SynchronizationContext.Current;
    private CancellationTokenSource? _cancel;
    private ITimer? _timer;
    private DateTimeOffset _nextRequestAt;
    private SyncProgress? _progress;

    private IReadOnlyList<int> _years = [];
    private int _selectedYear;
    private IReadOnlyList<DayTile> _days = [];
    private string _starsText = string.Empty;
    private string _lastSyncedText = "Not synced yet";
    private bool _isSyncing;
    private string _progressText = string.Empty;
    private int _seconds;
    private string _resultText = string.Empty;
    private IReadOnlyList<string> _errorLines = [];
    private bool _needsSignIn;

    public CalendarViewModel(SyncService sync, ISyncStateStore store, TimeProvider time, ILogger? logger = null)
    {
        _sync = sync;
        _store = store;
        _time = time;
        _log = logger ?? NullLogger.Instance;
    }

    public event EventHandler<DaySelectedEventArgs>? DaySelected;

    public event EventHandler? AccountRequested;

    public IReadOnlyList<int> Years { get => _years; private set => Set(ref _years, value); }

    public int SelectedYear { get => _selectedYear; private set => Set(ref _selectedYear, value); }

    public IReadOnlyList<DayTile> Days
    {
        get => _days;
        private set
        {
            Set(ref _days, value);
            Raise(nameof(IsEmpty));
        }
    }

    public bool IsEmpty => _days.Count == 0;

    public string StarsText { get => _starsText; private set => Set(ref _starsText, value); }

    public string LastSyncedText { get => _lastSyncedText; private set => Set(ref _lastSyncedText, value); }

    public bool IsSyncing
    {
        get => _isSyncing;
        private set
        {
            Set(ref _isSyncing, value);
            Raise(nameof(CanSync));
        }
    }

    public bool CanSync => !_isSyncing;

    public string ProgressText { get => _progressText; private set => Set(ref _progressText, value); }

    public int SecondsUntilNextRequest { get => _seconds; private set => Set(ref _seconds, value); }

    public string ResultText { get => _resultText; private set => Set(ref _resultText, value); }

    public IReadOnlyList<string> ErrorLines { get => _errorLines; private set => Set(ref _errorLines, value); }

    public bool NeedsSignIn { get => _needsSignIn; private set => Set(ref _needsSignIn, value); }

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        await RefreshYearsAsync(cancellationToken);
        await SelectYearAsync(Years[0], cancellationToken);
    }

    public async Task SelectYearAsync(int year, CancellationToken cancellationToken)
    {
        if (year == _selectedYear && !IsEmpty)
        {
            return;
        }

        SelectedYear = year;
        _log.YearShown(year);
        await ReloadAsync(cancellationToken);
    }

    public Task SyncYearAsync()
    {
        var year = _selectedYear;
        _log.SyncYearRequested(year);
        return RunAsync((progress, ct) => _sync.SyncYearAsync(year, refresh: true, progress, ct));
    }

    public Task SyncAllAsync()
    {
        _log.SyncAllRequested();
        var latest = CalendarRules.LatestEventYear(_time.GetUtcNow());
        return RunAsync((progress, ct) => _sync.SyncAllAsync([latest], progress, ct));
    }

    public void Cancel() => _cancel?.Cancel();

    public void Select(DayTile tile)
    {
        if (!tile.IsLocked)
        {
            _log.DaySelected(tile.Year, tile.Day);
            DaySelected?.Invoke(this, new DaySelectedEventArgs(tile.Year, tile.Day));
        }
    }

    public void RequestAccount() => AccountRequested?.Invoke(this, EventArgs.Empty);

    public void Dispose()
    {
        _timer?.Dispose();
        _cancel?.Dispose();
    }

    private async Task RefreshYearsAsync(CancellationToken cancellationToken)
    {
        var synced = await _store.SyncedYearsAsync(cancellationToken);
        var latest = Math.Max(CalendarRules.LatestEventYear(_time.GetUtcNow()), synced.DefaultIfEmpty().Max());
        var years = Enumerable.Range(CalendarRules.FirstEventYear, latest - CalendarRules.FirstEventYear + 1).Union(synced).OrderDescending().ToList();
        if (!years.SequenceEqual(Years))
        {
            Years = years;
        }
    }

    private async Task ReloadAsync(CancellationToken cancellationToken)
    {
        var year = _selectedYear;
        try
        {
            var stored = await _store.ReadYearAsync(year, cancellationToken);
            var lastSynced = await _store.LastSyncedAsync(year, cancellationToken);
            var now = _time.GetUtcNow();
            Days = [.. stored.OrderBy(d => d.Day).Select(d => new DayTile(year, d.Day, d.Stars, now))];
            StarsText = stored.Count == 0 ? string.Empty : $"{stored.Sum(d => d.Stars)} of {stored.Count * 2} stars";
            LastSyncedText = lastSynced is { } at ? $"Last synced {at.UtcDateTime:yyyy-MM-dd HH:mm} UTC" : "Not synced yet";
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _log.LoadFailed(year, ex);
            Days = [];
            StarsText = string.Empty;
            LastSyncedText = "The saved calendar could not be read";
        }
    }

    private async Task RunAsync(Func<IProgress<SyncProgress>, CancellationToken, Task<SyncSummary>> run)
    {
        if (_isSyncing)
        {
            return;
        }

        ResultText = string.Empty;
        ErrorLines = [];
        NeedsSignIn = false;
        _cancel = new CancellationTokenSource();
        IsSyncing = true;
        _timer = _time.CreateTimer(OnTimer, null, Tick, Tick);
        try
        {
            var summary = await run(new InlineProgress(OnProgress), _cancel.Token);
            Show(summary);
            _log.SyncFinished(summary.Outcome);
        }
        catch (OperationCanceledException)
        {
            ResultText = "Sync cancelled. Years already saved are kept.";
            _log.SyncCancelled();
        }
        finally
        {
            _timer.Dispose();
            _timer = null;
            _cancel.Dispose();
            _cancel = null;
            _progress = null;
            ProgressText = string.Empty;
            SecondsUntilNextRequest = 0;
            IsSyncing = false;
        }

        await RefreshYearsAsync(CancellationToken.None);
        await ReloadAsync(CancellationToken.None);
    }

    private void OnProgress(SyncProgress progress)
    {
        _progress = progress;
        _nextRequestAt = _time.GetUtcNow().AddSeconds(progress.SecondsUntilNextRequest);
        UpdateProgress();
    }

    private void OnTimer(object? state)
    {
        if (_context is null)
        {
            UpdateProgress();
        }
        else
        {
            _context.Post(_ => UpdateProgress(), null);
        }
    }

    private void UpdateProgress()
    {
        if (_progress is not { } p)
        {
            return;
        }

        SecondsUntilNextRequest = (int)Math.Ceiling(Math.Max(0, (_nextRequestAt - _time.GetUtcNow()).TotalSeconds));
        var wait = SecondsUntilNextRequest > 0 ? $"Next request in {SecondsUntilNextRequest} s." : "Requesting now.";
        ProgressText = $"Syncing {p.Year}, {Plural(p.YearsRemaining, "more year")} queued. {wait}";
    }

    private void Show(SyncSummary summary)
    {
        NeedsSignIn = summary.Outcome == SyncOutcome.NotSignedIn;
        ResultText = summary.Outcome switch
        {
            SyncOutcome.NotSignedIn => "You are not signed in to Advent of Code.",
            SyncOutcome.RateLimited => "Advent of Code asked for fewer requests. Try again later.",
            SyncOutcome.EventsUnavailable => "The list of years could not be loaded.",
            _ => $"{Plural(summary.YearsSynced.Count, "year")} synced, {Plural(summary.DaysUpdated, "day")} updated.",
        };
        ErrorLines = [.. summary.Errors.Select(e => $"{e.Year}: {Describe(e.Error)}")];
    }

    private static string Describe(SyncError error) => error switch
    {
        SyncError.ParseError => "the calendar page could not be read.",
        SyncError.NotFound => "the year was not found.",
        _ => "a network error stopped the request.",
    };

    private static string Plural(int count, string noun) => $"{count.ToString(CultureInfo.InvariantCulture)} {noun}{(count == 1 ? string.Empty : "s")}";

    private sealed class InlineProgress(Action<SyncProgress> report) : IProgress<SyncProgress>
    {
        public void Report(SyncProgress value) => report(value);
    }
}
