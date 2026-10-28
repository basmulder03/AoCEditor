namespace Session.Application;

public abstract record SecretResult
{
    public sealed record Ok(string? Value = null) : SecretResult
    {
        // The value is a secret; keep it out of ToString and logs.
        public override string ToString() => nameof(Ok);
    }

    public sealed record NotFound : SecretResult;

    public sealed record Unavailable(string Reason) : SecretResult;
}
