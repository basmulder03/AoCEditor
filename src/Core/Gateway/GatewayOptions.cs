namespace AoCEditor.Core.Gateway;

public sealed record GatewayOptions
{
    public static readonly TimeSpan InteractiveFloor = TimeSpan.FromSeconds(1);

    public static readonly TimeSpan StatisticsFloor = TimeSpan.FromMinutes(3);

    /// <summary>Minimum time between two sign-in prompts.</summary>
    public TimeSpan SignInPromptInterval { get; init; } = TimeSpan.FromSeconds(30);

    public Uri BaseAddress { get; init; } = new("https://adventofcode.com/");

    /// <summary>Raised to <see cref="InteractiveFloor"/> if lower.</summary>
    public TimeSpan InteractiveInterval { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>Raised to <see cref="StatisticsFloor"/> if lower.</summary>
    public TimeSpan StatisticsInterval { get; init; } = StatisticsFloor;

    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);

    public string RepositoryUrl { get; init; } = "https://github.com/basmulder03/AoCEditor";

    public string Maintainer { get; init; } = "github.com/basmulder03";
}
