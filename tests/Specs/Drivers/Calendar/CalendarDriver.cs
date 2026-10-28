using System.ComponentModel;
using Microsoft.Extensions.Time.Testing;
using Shared.Contracts;
using Sync.Application;
using Sync.UI;

namespace Specs.Drivers.Calendar;

public sealed class CalendarDriver
{
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 10, 28, 12, 0, 0, TimeSpan.Zero));
    private readonly Dictionary<int, IReadOnlyList<SyncedDay>> _stored = [];
    private readonly Dictionary<string, GatewayResult> _pages = [];
    private int _waitSeconds;

    public CalendarViewModel? Model { get; private set; }

    public List<string> ProgressTexts { get; } = [];

    public void SetToday(int year, int month, int day) => _time.SetUtcNow(new DateTimeOffset(year, month, day, 12, 0, 0, TimeSpan.Zero));

    public void SetWait(int seconds) => _waitSeconds = seconds;

    public void Store(int year, params (int Day, int Stars)[] stars) =>
        _stored[year] = [.. Enumerable.Range(1, year == 2026 ? 25 : 12).Select(d => new SyncedDay(d, stars.FirstOrDefault(s => s.Day == d).Stars))];

    public void Remote(int year, params (int Day, int Stars)[] stars) =>
        _pages[$"/{year}"] = new GatewayResult.Ok(string.Concat(Enumerable.Range(1, 12).Select(d =>
            $"<a href=\"/{year}/day/{d}\" class=\"calendar-day{d}{(stars.Any(s => s.Day == d && s.Stars == 2) ? " calendar-verycomplete" : string.Empty)}\">{d}</a>")));

    public async Task OpenAsync()
    {
        var store = new MemoryStore(_stored);
        Model = new CalendarViewModel(new SyncService(new Gateway(this), store, _time), store, _time);
        Model.PropertyChanged += OnChanged;
        await Model.LoadAsync(CancellationToken.None);
    }

    public Task SwitchAsync(int year) => Model!.SelectYearAsync(year, CancellationToken.None);

    public Task SyncYearAsync() => Model!.SyncYearAsync();

    public string LabelOf(int day) => Model!.Days.Single(d => d.Day == day).Label;

    private void OnChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CalendarViewModel.ProgressText) && Model!.ProgressText.Length > 0)
        {
            ProgressTexts.Add(Model.ProgressText);
        }
    }

    private sealed class Gateway(CalendarDriver owner) : IAocGateway
    {
        public Task<GatewayResult> GetAsync(string path, RequestKind kind, FetchPolicy policy, CancellationToken cancellationToken) =>
            Task.FromResult(owner._pages.GetValueOrDefault(path) ?? new GatewayResult.NotFound());

        public DateTimeOffset NextAllowedAt(RequestKind kind) => owner._time.GetUtcNow().AddSeconds(owner._waitSeconds);

        public Task<GatewayResult> CheckTokenAsync(string token, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<GatewayResult> PostFormAsync(string path, IReadOnlyDictionary<string, string> form, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class MemoryStore(Dictionary<int, IReadOnlyList<SyncedDay>> years) : ISyncStateStore
    {
        private readonly Dictionary<int, DateTimeOffset> _at = [];

        public Task<IReadOnlyList<SyncedDay>> ReadYearAsync(int year, CancellationToken cancellationToken) =>
            Task.FromResult(years.GetValueOrDefault(year) ?? []);

        public Task ReplaceYearAsync(int year, IReadOnlyList<SyncedDay> days, DateTimeOffset syncedAt, CancellationToken cancellationToken)
        {
            years[year] = days;
            _at[year] = syncedAt;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<int>> SyncedYearsAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<int>>([.. years.Keys]);

        public Task<DateTimeOffset?> LastSyncedAsync(int year, CancellationToken cancellationToken) =>
            Task.FromResult<DateTimeOffset?>(_at.TryGetValue(year, out var at) ? at : null);
    }
}
