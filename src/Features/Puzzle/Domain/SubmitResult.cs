using System.Globalization;
using System.Text.RegularExpressions;

namespace Puzzle.Domain;

/// <summary>Wait is only set for RateLimited, and null when the response gave no readable time.</summary>
public sealed record SubmitResult(SubmitOutcome Outcome, TimeSpan? Wait = null);
