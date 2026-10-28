using Diagnostics.Application;
using Shared.Contracts;

namespace Diagnostics.Tests;

internal sealed class MemoryStore : IKeyValueStore
{
    public Dictionary<string, string> Items { get; } = [];

    public Task<string?> GetAsync(string key, CancellationToken cancellationToken) =>
        Task.FromResult(Items.TryGetValue(key, out var v) ? v : null);

    public Task SetAsync(string key, string value, CancellationToken cancellationToken)
    {
        Items[key] = value;
        return Task.CompletedTask;
    }
}

internal sealed class ListSink : ILogSink
{
    public List<LogEntry> Entries { get; } = [];

    public void Write(LogEntry entry) => Entries.Add(entry);
}

internal static class LoggerExtras
{
    public static void Say(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.Extensions.Logging.LogLevel level, string message, Exception? exception = null) =>
        logger.Log(level, default, message, exception, static (s, _) => s);
}
