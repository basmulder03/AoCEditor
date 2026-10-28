namespace AoCEditor.Core.Puzzle.Domain;

public sealed record ListBlock(IReadOnlyList<IReadOnlyList<Fragment>> Items) : Block;
