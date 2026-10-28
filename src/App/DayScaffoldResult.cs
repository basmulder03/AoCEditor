using System.Globalization;
using Plugins.Domain;
using Puzzle.Application;
using Runner.Application;
using Shared.Contracts;
using Workspace.Application;
using Workspace.Domain;

namespace AoCEditor;

/// <summary>Folder and SourcePath are set when Kind is Scaffolded.</summary>
public sealed record DayScaffoldResult(DayScaffoldKind Kind, string? Folder = null, string? SourcePath = null);
