using AoCEditor.Core.Plugins.Domain;
using AoCEditor.Core.Puzzle.Application;
using AoCEditor.Core.Session.Application;
using AoCEditor.Core.Sync.Application;
using AoCEditor.Infrastructure.Session;
using AoCEditor.Infrastructure.Storage;
using AoCEditor.UI.Session;
using Microsoft.Extensions.Logging;
using Shared.Contracts;

namespace AoCEditor;

internal static partial class AppLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = "AoCEditor {Version} started, debug mode {Debug}")]
    public static partial void AppStarted(this ILogger logger, string version, bool debug);
}
