namespace Shared;

/// <summary>Facts about the Advent of Code event calendar that every slice agrees on.</summary>
public static class AocEvent
{
    public const int FirstYear = 2015;

    /// <summary>Days unlock at midnight in this fixed offset (UTC-5), with no daylight saving.</summary>
    public static readonly TimeSpan UnlockOffset = TimeSpan.FromHours(-5);
}
