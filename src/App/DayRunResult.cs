using System.Globalization;
using Plugins.Domain;
using Puzzle.Application;
using Runner.Application;
using Shared.Contracts;
using Workspace.Application;
using Workspace.Domain;

namespace AoCEditor;

/// <summary>Run is the result of the last command that started; Detail is a sentence for the user when the run did not go ahead.</summary>
public sealed record DayRunResult(DayRunKind Kind, RunResult? Run = null, string? Detail = null);
