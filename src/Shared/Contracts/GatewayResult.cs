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

    public sealed record NetworkError : GatewayResult;
}
