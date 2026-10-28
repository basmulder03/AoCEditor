using Diagnostics.Application;
using Gateway;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Plugins.Application;
using Plugins.Domain;
using Puzzle.Application;
using Runner.Application;
using Session.Application;
using Session.Infrastructure;
using Session.UI;
using Shared.Contracts;
using Storage.Infrastructure;
using Sync.Application;
using Workspace.Application;

namespace AoCEditor;

/// <summary>Replacements for the machine-specific parts of the graph; null keeps the real one.</summary>
public sealed record AppOverrides(ISecretStore? Secrets = null, IAppDataPaths? Paths = null, DebugMode? Debug = null);
