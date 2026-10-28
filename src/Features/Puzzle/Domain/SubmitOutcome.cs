using System.Globalization;
using System.Text.RegularExpressions;

namespace Puzzle.Domain;

public enum SubmitOutcome { Unknown, Correct, Wrong, TooHigh, TooLow, RateLimited, AlreadySolved }
