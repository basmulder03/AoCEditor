using System.Globalization;
using AoCEditor.Core.Plugins.Domain;
using AoCEditor.Core.Puzzle.Application;
using AoCEditor.Core.Workspace.Domain;
using Shared.Contracts;

namespace AoCEditor;

/// <summary>Folder and SourcePath are set when Kind is Scaffolded.</summary>
public sealed record DayScaffoldResult(DayScaffoldKind Kind, string? Folder = null, string? SourcePath = null);
