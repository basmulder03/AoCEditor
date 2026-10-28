using System.Globalization;
using Puzzle.Domain;
using Shared.Contracts;

namespace Puzzle.Application;

/// <summary>Unlocked is the refreshed puzzle after a correct part 1, null otherwise or when that refresh failed.</summary>
public sealed record Submission(SubmitResult Verdict, PuzzleDocument? Unlocked);
