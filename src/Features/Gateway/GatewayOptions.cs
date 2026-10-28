namespace Gateway;

public sealed record GatewayOptions
{
    public Uri BaseAddress { get; init; } = new("https://adventofcode.com/");

    public TimeSpan MinInterval { get; init; } = TimeSpan.FromSeconds(5);

    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);

    public string RepositoryUrl { get; init; } = "https://github.com/basmulder03/AoCEditor";
}
