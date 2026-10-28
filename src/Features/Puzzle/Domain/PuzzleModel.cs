namespace Puzzle.Domain;

public enum InlineStyle { Plain, Emphasis, Code, Link }

public sealed record Inline(string Text, InlineStyle Style = InlineStyle.Plain, string? Href = null);

public abstract record Block;

public sealed record ParagraphBlock(IReadOnlyList<Inline> Inlines) : Block;

public sealed record ListBlock(IReadOnlyList<IReadOnlyList<Inline>> Items) : Block;

public sealed record CodeBlock(string Text) : Block;

public sealed record PuzzlePart(IReadOnlyList<Block> Blocks);

/// <summary>Answers lists the "Your puzzle answer was" values in part order; it is shorter than Parts while parts are unsolved.</summary>
public sealed record PuzzleDocument(string Title, IReadOnlyList<PuzzlePart> Parts, IReadOnlyList<string> Answers);
