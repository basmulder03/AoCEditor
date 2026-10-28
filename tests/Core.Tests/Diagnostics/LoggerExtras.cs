using Diagnostics.Application;
using Shared.Contracts;

namespace Diagnostics.Tests;

internal static class LoggerExtras
{
    public static void Say(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.Extensions.Logging.LogLevel level, string message, Exception? exception = null) =>
        logger.Log(level, default, message, exception, static (s, _) => s);
}
