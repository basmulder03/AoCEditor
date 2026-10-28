using System.Globalization;
using AoCEditor.Core.Puzzle.Domain;
using Shared.Contracts;

namespace AoCEditor.Core.Puzzle.Application;

/// <summary>Reason is the gateway's network failure text, never a secret.</summary>
public sealed record Fetched<T>(FetchStatus Status, T? Value = default, string? Reason = null);
