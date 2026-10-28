namespace AoCEditor.Core.Session.Application;

/// <summary>Lets the user sign in on the AoC site and hands back the resulting session token.</summary>
public interface ISignInBrowser
{
    /// <summary>
    /// Waits until the user is signed in. Every token found in the browser is offered to <paramref name="validate"/>;
    /// only an accepted one ends the wait, so a stale cookie keeps the browser open.
    /// </summary>
    Task<BrowserOutcome> AcquireTokenAsync(TokenValidator validate, CancellationToken cancellationToken);
}
