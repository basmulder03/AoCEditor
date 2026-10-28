namespace AoCEditor.Core.Puzzle.Domain;

public sealed record ParagraphBlock(IReadOnlyList<Fragment> Inlines) : Block;
