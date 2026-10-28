using Shared.Contracts;

namespace AoCEditor.Infrastructure.Tests.Diagnostics;

internal static class LoggerExtras
{
    public static void Say(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.Extensions.Logging.LogLevel level, string message, Exception? exception = null) =>
        logger.Log(level, default, message, exception, static (s, _) => s);
}
