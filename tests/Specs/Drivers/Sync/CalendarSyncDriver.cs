using Microsoft.Extensions.Logging.Abstractions;
using AoCEditor.Core.Sync.Application;
using AoCEditor.Core.Sync.Domain;
using Microsoft.Extensions.Time.Testing;
using Shared.Contracts;

namespace Specs.Drivers.Sync;

public sealed class CalendarSyncDriver
{
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 10, 28, 0, 0, 0, TimeSpan.Zero));
    private readonly Dictionary<string, GatewayResult> _pages = [];
    private readonly Dictionary<int, IReadOnlyList<SyncedDay>> _stored = [];
    private readonly Gateway _gateway;
    private readonly Store _store;
    private bool _signedIn = true;
    private List<int> _years = [];

    public CalendarSyncDriver()
    {
        _gateway = new Gateway(this);
        _store = new Store(this);
    }

    public List<string> Requests { get; } = [];

    public SyncSummary? Summary { get; private set; }

    public void MarkSynced(int year) => _stored[year] = [];

    public void SignOut() => _signedIn = false;

    public void SetToday(int year, int month, int day) => _time.SetUtcNow(new DateTimeOffset(year, month, day, 12, 0, 0, TimeSpan.Zero));

    public void RunEvents(params int[] years)
    {
        _years = [.. years];
        foreach (var year in years)
        {
            _pages[$"/{year}"] = new GatewayResult.Ok(Calendar(year, []));
        }
    }

    public void SetStars(int year, params (int Day, int Stars)[] days) => _pages[$"/{year}"] = new GatewayResult.Ok(Calendar(year, days));

    public void ShowUnexpectedPage(int year) => _pages[$"/{year}"] = new GatewayResult.Ok("<html>maintenance</html>");

    public async Task SyncAsync()
    {
        var events = string.Concat(_years.OrderDescending().Select(y => $"<div class=\"eventlist-event\"><a href=\"/{y}\">[{y}]</a></div>"));
        _pages["/events"] = new GatewayResult.Ok(events);
        Summary = await new SyncService(_gateway, _store, _time, NullLogger<SyncService>.Instance).SyncAllAsync([], new Progress<SyncProgress>(), CancellationToken.None);
    }

    public DayState StateOf(int year, int day) =>
        CalendarRules.StateOf(year, day, _stored[year].FirstOrDefault(d => d.Day == day)?.Stars ?? 0, _time.GetUtcNow());

    private static string Calendar(int year, (int Day, int Stars)[] days)
    {
        var known = days.ToDictionary(d => d.Day, d => d.Stars);
        var links = Enumerable.Range(1, 25).Select(d => known.GetValueOrDefault(d) switch
        {
            1 => $"<a href=\"/{year}/day/{d}\" class=\"calendar-day{d} calendar-complete\">{d}</a>",
            2 => $"<a href=\"/{year}/day/{d}\" class=\"calendar-day{d} calendar-verycomplete\">{d}</a>",
            _ => $"<a href=\"/{year}/day/{d}\" class=\"calendar-day{d}\">{d}</a>",
        });
        return string.Concat(links);
    }

    private sealed class Gateway(CalendarSyncDriver owner) : IAocGateway
    {
        public Task<GatewayResult> GetAsync(string path, RequestKind kind, FetchPolicy policy, CancellationToken cancellationToken)
        {
            owner.Requests.Add(path);
            GatewayResult result = !owner._signedIn ? new GatewayResult.NotSignedIn()
                : owner._pages.GetValueOrDefault(path) ?? new GatewayResult.NotFound();
            return Task.FromResult(result);
        }

        public DateTimeOffset NextAllowedAt(RequestKind kind) => owner._time.GetUtcNow();

        public Task<GatewayResult> CheckTokenAsync(string token, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<GatewayResult> PostFormAsync(string path, IReadOnlyDictionary<string, string> form, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class Store(CalendarSyncDriver owner) : ISyncStateStore
    {
        public Task<IReadOnlyList<SyncedDay>> ReadYearAsync(int year, CancellationToken cancellationToken) =>
            Task.FromResult(owner._stored.GetValueOrDefault(year) ?? []);

        public Task ReplaceYearAsync(int year, IReadOnlyList<SyncedDay> days, DateTimeOffset syncedAt, CancellationToken cancellationToken)
        {
            owner._stored[year] = days;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<int>> SyncedYearsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<int>>([.. owner._stored.Keys]);

        public Task<DateTimeOffset?> LastSyncedAsync(int year, CancellationToken cancellationToken) =>
            Task.FromResult<DateTimeOffset?>(null);
    }
}
