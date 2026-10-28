using System.Globalization;
using Puzzle.Domain;
using Shared.Contracts;

namespace Puzzle.Application;

/// <summary>Reason is the gateway's network failure text, never a secret.</summary>
public sealed record Fetched<T>(FetchStatus Status, T? Value = default, string? Reason = null);
