namespace Puzzle.Domain;

public sealed record ListBlock(IReadOnlyList<IReadOnlyList<Fragment>> Items) : Block;
