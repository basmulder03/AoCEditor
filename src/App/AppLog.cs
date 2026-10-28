using Diagnostics.Application;
using Gateway;
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

internal static partial class AppLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = "AoCEditor {Version} started, debug mode {Debug}")]
    public static partial void AppStarted(this ILogger logger, string version, bool debug);
}
