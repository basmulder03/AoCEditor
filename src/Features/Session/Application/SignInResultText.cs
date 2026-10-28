namespace Session.Application;

/// <summary>The message shown to the user for a sign-in result, with the real reason where there is one.</summary>
public static class SignInResultText
{
    public static string For(SignInResult result) => result switch
    {
        SignInResult.SignedIn => "Signed in.",
        SignInResult.Cancelled => "Sign-in was cancelled. You can paste your session token instead.",
        SignInResult.TimedOut => "Sign-in took too long and was stopped. You can paste your session token instead.",
        SignInResult.InvalidToken => "Advent of Code did not accept that token. Check that you copied the whole session value.",
        SignInResult.StoreUnavailable u => $"Secure storage is unavailable: {u.Reason}",
        SignInResult.NetworkError { Reason: { Length: > 0 } reason } => $"Could not reach Advent of Code ({reason}). Try again in a moment.",
        SignInResult.NetworkError => "Could not reach Advent of Code. Try again in a moment.",
        SignInResult.BrowserUnsupported u => $"The sign-in window could not be used: {u.Reason}. Paste your session token instead.",
        _ => string.Empty,
    };
}
