using Microsoft.Extensions.Logging;

namespace Gateway;

internal static partial class GatewayLog
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Cache hit for {Path}")]
    public static partial void CacheHit(this ILogger logger, string path);

    [LoggerMessage(Level = LogLevel.Information, Message = "No session token; {Method} {Path} not sent")]
    public static partial void NoToken(this ILogger logger, string method, string path);

    [LoggerMessage(Level = LogLevel.Information, Message = "{Method} {Path} -> {Status} in {Ms} ms (throttle wait {WaitMs} ms)")]
    public static partial void RequestDone(this ILogger logger, string method, string path, int? status, long ms, long waitMs);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{Method} {Path} failed: {Reason} after {Ms} ms")]
    public static partial void RequestFailed(this ILogger logger, string method, string path, string reason, long ms);

    [LoggerMessage(Level = LogLevel.Information, Message = "Sign-in required prompt raised")]
    public static partial void SignInPrompted(this ILogger logger);
}
