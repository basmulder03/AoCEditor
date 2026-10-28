namespace AoCEditor.Core.Puzzle.Domain;

public sealed record Fragment(string Text, InlineStyle Style = InlineStyle.Plain, string? Href = null);
