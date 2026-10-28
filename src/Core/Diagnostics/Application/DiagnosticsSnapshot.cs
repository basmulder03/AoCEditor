using System.Globalization;
using System.Text;
using AoCEditor.Core.Diagnostics.Domain;
using Shared.Contracts;

namespace AoCEditor.Core.Diagnostics.Application;

public sealed record DiagnosticsSnapshot(
    string AppVersion,
    string Os,
    string Runtime,
    string SessionState,
    StorageStatus Storage,
    IReadOnlyList<GatewayRequestEntry> Requests,
    IReadOnlyList<LogEntry> Logs);
