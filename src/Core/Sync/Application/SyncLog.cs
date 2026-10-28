using Microsoft.Extensions.Logging;

namespace AoCEditor.Core.Sync.Application;

internal static partial class SyncLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Sync of all years started")]
    public static partial void SyncAllStarted(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Year {Year} already synced; skipped")]
    public static partial void YearSkipped(this ILogger logger, int year);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Event list unavailable: {Reason}")]
    public static partial void EventsUnavailable(this ILogger logger, string reason);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Event list could not be read")]
    public static partial void EventsUnreadable(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Syncing year {Year}, {Remaining} more queued")]
    public static partial void YearStarted(this ILogger logger, int year, int remaining);

    [LoggerMessage(Level = LogLevel.Information, Message = "Year {Year} synced, {Days} days")]
    public static partial void YearSynced(this ILogger logger, int year, int days);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Calendar of year {Year} could not be read")]
    public static partial void YearUnreadable(this ILogger logger, int year);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Sync stopped at year {Year}: {Outcome}")]
    public static partial void SyncAborted(this ILogger logger, int year, SyncOutcome outcome);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Year {Year} failed: {Reason}")]
    public static partial void YearFailed(this ILogger logger, int year, string reason);
}
