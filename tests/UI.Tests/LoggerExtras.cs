namespace AoCEditor.UI.Tests;

internal static class LoggerExtras
{
    public static void Say(this Microsoft.Extensions.Logging.ILogger logger, Microsoft.Extensions.Logging.LogLevel level, string message) =>
        logger.Log(level, default, message, null, static (s, _) => s);
}
