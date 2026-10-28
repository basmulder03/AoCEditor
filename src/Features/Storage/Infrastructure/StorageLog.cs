using System.Globalization;
using System.Reflection;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Storage.Infrastructure;

internal static partial class StorageLog
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Database has {Applied} migrations applied, {Pending} pending")]
    public static partial void MigrationState(this ILogger logger, int applied, int pending);

    [LoggerMessage(Level = LogLevel.Information, Message = "Applying migration {Version}")]
    public static partial void MigrationApplying(this ILogger logger, int version);

    [LoggerMessage(Level = LogLevel.Information, Message = "Migration {Version} applied")]
    public static partial void MigrationApplied(this ILogger logger, int version);

    [LoggerMessage(Level = LogLevel.Error, Message = "Migration {Version} failed: {Reason}")]
    public static partial void MigrationFailed(this ILogger logger, int version, string reason);

    [LoggerMessage(Level = LogLevel.Error, Message = "Database could not be opened: {Reason}")]
    public static partial void OpenFailed(this ILogger logger, string reason);
}
