using Microsoft.Extensions.Logging;

namespace Session.Application;

internal static partial class SessionLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Browser sign-in started")]
    public static partial void BrowserSignInStarted(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Pasted-token sign-in started")]
    public static partial void PasteSignInStarted(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Session candidate checked: {Verdict}")]
    public static partial void CandidateChecked(this ILogger logger, CandidateVerdict verdict);

    [LoggerMessage(Level = LogLevel.Information, Message = "Sign-in outcome: {Outcome}")]
    public static partial void Outcome(this ILogger logger, string outcome);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Sign-in browser unsupported: {Reason}")]
    public static partial void BrowserUnsupported(this ILogger logger, string reason);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Secure storage unavailable: {Reason}")]
    public static partial void StoreUnavailable(this ILogger logger, string reason);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Sign-in failed on the network: {Reason}")]
    public static partial void NetworkFailed(this ILogger logger, string reason);

    [LoggerMessage(Level = LogLevel.Information, Message = "Signed out")]
    public static partial void SignedOut(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Browser window opened for {Page}")]
    public static partial void WindowOpening(this ILogger logger, string page);

    [LoggerMessage(Level = LogLevel.Information, Message = "Browser navigation finished: {Page} success={Success}")]
    public static partial void Navigated(this ILogger logger, string page, bool success);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Cookie manager available={Available}, {Count} cookies, session cookie present={Present}")]
    public static partial void CookiesRead(this ILogger logger, bool available, int count, bool present);

    [LoggerMessage(Level = LogLevel.Information, Message = "No session candidate on {Page}; waiting")]
    public static partial void NoCandidate(this ILogger logger, string page);

    [LoggerMessage(Level = LogLevel.Information, Message = "Browser window closed by the user")]
    public static partial void WindowClosed(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Browser failure while {Stage}")]
    public static partial void BrowserFailure(this ILogger logger, Exception exception, string stage);
}
