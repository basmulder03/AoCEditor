namespace Shared.Contracts;

/// <summary>Each kind has its own minimum spacing between requests.</summary>
public enum RequestKind
{
    /// <summary>Puzzle text, input and answer submission.</summary>
    Interactive,

    /// <summary>Events list and calendar pages, which scrape statistics.</summary>
    Statistics,
}
