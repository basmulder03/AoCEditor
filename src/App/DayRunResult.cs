using System.Globalization;
using AoCEditor.Core.Plugins.Domain;
using AoCEditor.Core.Puzzle.Application;
using AoCEditor.Core.Runner.Application;
using AoCEditor.Core.Workspace.Domain;
using Shared.Contracts;

namespace AoCEditor;

/// <summary>Run is the result of the last command that started; Detail is a sentence for the user when the run did not go ahead.</summary>
public sealed record DayRunResult(DayRunKind Kind, RunResult? Run = null, string? Detail = null);
