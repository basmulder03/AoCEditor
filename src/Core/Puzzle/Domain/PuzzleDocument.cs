namespace AoCEditor.Core.Puzzle.Domain;

/// <summary>Answers lists the "Your puzzle answer was" values in part order; it is shorter than Parts while parts are unsolved.</summary>
public sealed record PuzzleDocument(string Title, IReadOnlyList<PuzzlePart> Parts, IReadOnlyList<string> Answers);
