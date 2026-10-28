using System.Globalization;
using AoCEditor.Core.Puzzle.Domain;
using Shared.Contracts;

namespace AoCEditor.Core.Puzzle.Application;

public enum FetchStatus { Ok, NotSignedIn, RateLimited, NotFound, NetworkError, Unreadable, CoolingDown, AlreadyTried }
