using System.Globalization;
using System.Text;
using Diagnostics.Domain;
using Shared.Contracts;

namespace Diagnostics.Application;

public sealed record DiagnosticsSnapshot(
    string AppVersion,
    string Os,
    string Runtime,
    string SessionState,
    StorageStatus Storage,
    IReadOnlyList<GatewayRequestEntry> Requests,
    IReadOnlyList<LogEntry> Logs);
