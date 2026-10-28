using System.Globalization;
using Puzzle.Domain;
using Shared.Contracts;

namespace Puzzle.Application;

public enum FetchStatus { Ok, NotSignedIn, RateLimited, NotFound, NetworkError, Unreadable, CoolingDown, AlreadyTried }
