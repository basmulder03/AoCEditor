using System.Globalization;
using System.Text.RegularExpressions;

namespace AoCEditor.Core.Puzzle.Domain;

public enum SubmitOutcome { Unknown, Correct, Wrong, TooHigh, TooLow, RateLimited, AlreadySolved }
