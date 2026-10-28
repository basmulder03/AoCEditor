using Shared.Contracts;

namespace Session.Application;

/// <summary>Obtains a session token, proves it with one request and only then stores it.</summary>
public sealed class SignInCoordinator(
    SessionTokenProvider tokens,
    IAocGateway gateway,
    Action? clearBrowserProfile = null,
    TimeProvider? time = null,
    TimeSpan? browserTimeout = null)
{
    public static readonly TimeSpan DefaultBrowserTimeout = TimeSpan.FromMinutes(10);

    public async Task<SignInResult> SignInWithBrowserAsync(ISignInBrowser browser, CancellationToken cancellationToken)
    {
        if (await StoreProblemAsync(cancellationToken) is { } problem)
        {
            return problem;
        }

        using var limit = new CancellationTokenSource(browserTimeout ?? DefaultBrowserTimeout, time ?? TimeProvider.System);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, limit.Token);
        string? accepted = null;
        var outcome = await browser.AcquireTokenAsync(
            async (candidate, ct) =>
            {
                var verdict = ToVerdict(await gateway.CheckTokenAsync(candidate, ct));
                if (verdict == CandidateVerdict.Accepted)
                {
                    accepted = candidate;
                }

                return verdict;
            },
            linked.Token);

        return outcome switch
        {
            BrowserOutcome.Token t => await ValidateAndSaveAsync(t.Value, cancellationToken, alreadyChecked: t.Value == accepted),
            BrowserOutcome.Unsupported u => new SignInResult.BrowserUnsupported(u.Reason),
            _ when limit.IsCancellationRequested && !cancellationToken.IsCancellationRequested => new SignInResult.TimedOut(),
            _ => new SignInResult.Cancelled(),
        };
    }

    private static CandidateVerdict ToVerdict(GatewayResult result) => result switch
    {
        GatewayResult.Ok => CandidateVerdict.Accepted,
        GatewayResult.NotSignedIn => CandidateVerdict.Rejected,
        _ => CandidateVerdict.Unreachable,
    };

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

    private async Task<SignInResult> ValidateAndSaveAsync(string raw, CancellationToken cancellationToken, bool alreadyChecked = false)
    {
        if (Clean(raw) is not { } token)
        {
            return new SignInResult.InvalidToken();
        }

        if (!alreadyChecked)
        {
            switch (await gateway.CheckTokenAsync(token, cancellationToken))
            {
                case GatewayResult.Ok:
                    break;
                case GatewayResult.NotSignedIn:
                    return new SignInResult.InvalidToken();
                default:
                    return new SignInResult.NetworkError();
            }
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
