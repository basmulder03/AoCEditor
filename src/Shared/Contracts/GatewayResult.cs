namespace Shared.Contracts;

public abstract record GatewayResult
{
    private GatewayResult()
    {
    }

    public sealed record Ok(string Content) : GatewayResult;

    public sealed record NotSignedIn : GatewayResult;

    public sealed record RateLimited : GatewayResult;

    public sealed record NotFound : GatewayResult;

    /// <summary>The request failed; <paramref name="Reason"/> says how, without any secret.</summary>
    public sealed record NetworkError(string? Reason = null) : GatewayResult;
}
