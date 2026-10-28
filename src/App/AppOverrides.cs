using AoCEditor.Core.Diagnostics.Application;
using AoCEditor.Core.Plugins.Domain;
using AoCEditor.Core.Puzzle.Application;
using AoCEditor.Core.Session.Application;
using AoCEditor.Core.Sync.Application;
using AoCEditor.Infrastructure.Session;
using AoCEditor.Infrastructure.Storage;
using AoCEditor.UI.Session;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Contracts;

namespace AoCEditor;

/// <summary>Replacements for the machine-specific parts of the graph; null keeps the real one.</summary>
public sealed record AppOverrides(ISecretStore? Secrets = null, IAppDataPaths? Paths = null, DebugMode? Debug = null);
