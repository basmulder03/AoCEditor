namespace AoCEditor.Core.Session.Application;

public abstract record BrowserOutcome
{
    private BrowserOutcome()
    {
    }

    public sealed record Token(string Value) : BrowserOutcome
    {
        // The value is a secret; keep it out of ToString and logs.
        public override string ToString() => nameof(Token);
    }

    /// <summary>The user closed the browser before signing in.</summary>
    public sealed record Cancelled : BrowserOutcome;

    /// <summary>The embedded browser cannot run here; the user can paste the token instead.</summary>
    public sealed record Unsupported(string Reason) : BrowserOutcome;
}
