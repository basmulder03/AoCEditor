using Shared.Contracts;

namespace Session.Application;

/// <summary>Obtains a session token, proves it with one request and only then stores it.</summary>
public sealed class SignInCoordinator(SessionTokenProvider tokens, IAocGateway gateway, Action? clearBrowserProfile = null)
{
    public async Task<SignInResult> SignInWithBrowserAsync(ISignInBrowser browser, CancellationToken cancellationToken)
    {
        if (await StoreProblemAsync(cancellationToken) is { } problem)
        {
            return problem;
        }

        return await browser.AcquireTokenAsync(cancellationToken) switch
        {
            BrowserOutcome.Token t => await ValidateAndSaveAsync(t.Value, cancellationToken),
            BrowserOutcome.Unsupported u => new SignInResult.BrowserUnsupported(u.Reason),
            _ => new SignInResult.Cancelled(),
        };
    }

    public async Task<SignInResult> SignInWithPastedTokenAsync(string pasted, CancellationToken cancellationToken)
    {
        if (await StoreProblemAsync(cancellationToken) is { } problem)
        {
            return problem;
        }

        return await ValidateAndSaveAsync(pasted, cancellationToken);
    }

    public async Task<SecretResult> SignOutAsync(CancellationToken cancellationToken)
    {
        var result = await tokens.ClearAsync(cancellationToken);
        clearBrowserProfile?.Invoke();
        return result;
    }

    public async Task<SessionStatus> GetStatusAsync(CancellationToken cancellationToken) =>
        await tokens.ReadAsync(cancellationToken) switch
        {
            SecretResult.Ok { Value: { Length: > 0 } } => new SessionStatus(SessionState.SignedIn),
            SecretResult.Unavailable u => new SessionStatus(SessionState.StoreUnavailable, u.Reason),
            _ => new SessionStatus(SessionState.SignedOut),
        };

    private async Task<SignInResult?> StoreProblemAsync(CancellationToken cancellationToken) =>
        await tokens.ReadAsync(cancellationToken) is SecretResult.Unavailable u ? new SignInResult.StoreUnavailable(u.Reason) : null;

    private async Task<SignInResult> ValidateAndSaveAsync(string raw, CancellationToken cancellationToken)
    {
        if (Clean(raw) is not { } token)
        {
            return new SignInResult.InvalidToken();
        }

        switch (await gateway.CheckTokenAsync(token, cancellationToken))
        {
            case GatewayResult.Ok:
                break;
            case GatewayResult.NotSignedIn:
                return new SignInResult.InvalidToken();
            default:
                return new SignInResult.NetworkError();
        }

        return await tokens.SaveAsync(token, cancellationToken) switch
        {
            SecretResult.Unavailable u => new SignInResult.StoreUnavailable(u.Reason),
            _ => new SignInResult.SignedIn(),
        };
    }

    // Accepts what people paste from dev tools: surrounding whitespace, quotes and a "session=" prefix.
    private static string? Clean(string raw)
    {
        var token = raw.Trim().Trim('"', '\'').Trim();
        if (token.StartsWith("session=", StringComparison.OrdinalIgnoreCase))
        {
            token = token["session=".Length..].Trim();
        }

        return token.Length > 0 && token.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.' or '~' or '%' or '+' or '/' or '=') ? token : null;
    }
}
