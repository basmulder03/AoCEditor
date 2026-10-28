using Microsoft.Extensions.Logging;

namespace AoCEditor.UI.Sync;

internal static partial class CalendarLog
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Calendar showing year {Year}")]
    public static partial void YearShown(this ILogger logger, int year);

    [LoggerMessage(Level = LogLevel.Information, Message = "Sync of year {Year} requested")]
    public static partial void SyncYearRequested(this ILogger logger, int year);

    [LoggerMessage(Level = LogLevel.Information, Message = "Sync of all years requested")]
    public static partial void SyncAllRequested(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Sync finished: {Outcome}")]
    public static partial void SyncFinished(this ILogger logger, AoCEditor.Core.Sync.Application.SyncOutcome outcome);

    [LoggerMessage(Level = LogLevel.Information, Message = "Sync cancelled by the user")]
    public static partial void SyncCancelled(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Day {Day} of {Year} selected")]
    public static partial void DaySelected(this ILogger logger, int year, int day);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Saved calendar of year {Year} could not be read")]
    public static partial void LoadFailed(this ILogger logger, int year, Exception exception);
}
