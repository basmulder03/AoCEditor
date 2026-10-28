namespace Session.Application;

public abstract record SignInResult
{
    private SignInResult()
    {
    }

    public sealed record SignedIn : SignInResult;

    public sealed record Cancelled : SignInResult;

    /// <summary>The site did not accept the token, or it is not shaped like a session token.</summary>
    public sealed record InvalidToken : SignInResult;

    /// <summary>There is nowhere safe to keep the token, so nothing was attempted.</summary>
    public sealed record StoreUnavailable(string Reason) : SignInResult;

    public sealed record NetworkError : SignInResult;

    public sealed record BrowserUnsupported(string Reason) : SignInResult;
}

public enum SessionState
{
    SignedOut,
    SignedIn,
    StoreUnavailable,
}

public sealed record SessionStatus(SessionState State, string? Reason = null);
