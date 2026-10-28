using Shared.Contracts;

namespace Sync.Tests;

internal static class Html
{
    public static string Events(params string[] hrefs) =>
        "<article>" + string.Concat(hrefs.Select(h => $"<div class=\"eventlist-event\"><a href=\"{h}\">[x]</a>  </div>")) + "</article>";

    /// <summary>Calendar with a day link per entry; stars 0, 1 or 2 map to the class names AoC uses.</summary>
    public static string Calendar(int year, params (int Day, int Stars)[] days) =>
        "<pre class=\"calendar\">" + string.Concat(days.Select(d =>
            $"<a aria-label=\"Day {d.Day}\" href=\"/{year}/day/{d.Day}\" class=\"calendar-day{d.Day}{Marks(d.Stars)}\"><span class=\"calendar-day\">{d.Day}</span></a>\n")) + "</pre>";

    private static string Marks(int stars) => stars switch
    {
        1 => " calendar-complete",
        2 => " calendar-verycomplete",
        _ => string.Empty,
    };
}

internal sealed class FakeGateway : IAocGateway
{
    public Dictionary<string, GatewayResult> Pages { get; } = [];

    public List<(string Path, RequestKind Kind, FetchPolicy Policy)> Requests { get; } = [];

    public DateTimeOffset NextAllowed { get; set; }

    /// <summary>When set, requests wait on it, like a gateway waiting for its spacing.</summary>
    public bool Hold { get; set; }

    public DateTimeOffset NextAllowedAt(RequestKind kind) => NextAllowed;

    public async Task<GatewayResult> GetAsync(string path, RequestKind kind, FetchPolicy policy, CancellationToken cancellationToken)
    {
        Requests.Add((path, kind, policy));
        if (Hold)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }

        return Pages.TryGetValue(path, out var r) ? r : new GatewayResult.NotFound();
    }

    public Task<GatewayResult> PostFormAsync(string path, IReadOnlyDictionary<string, string> form, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<GatewayResult> CheckTokenAsync(string token, CancellationToken cancellationToken) =>
        throw new NotSupportedException();
}

internal sealed class MemoryStore : ISyncStateStore
{
    public Dictionary<int, List<SyncedDay>> Years { get; } = [];

    public Task<IReadOnlyList<SyncedDay>> ReadYearAsync(int year, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<SyncedDay>>(Years.TryGetValue(year, out var d) ? d : []);

    public Task ReplaceYearAsync(int year, IReadOnlyList<SyncedDay> days, DateTimeOffset syncedAt, CancellationToken cancellationToken)
    {
        Years[year] = [.. days];
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<int>> SyncedYearsAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<int>>([.. Years.Keys]);

    public Task<DateTimeOffset?> LastSyncedAsync(int year, CancellationToken cancellationToken) =>
        Task.FromResult<DateTimeOffset?>(null);
}

internal sealed class CapturingLogger : Microsoft.Extensions.Logging.ILogger
{
    public List<string> Lines { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;

    public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
        Lines.Add($"{logLevel} {formatter(state, exception)}");
}
