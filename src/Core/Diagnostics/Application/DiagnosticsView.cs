using AoCEditor.Core.Diagnostics.Domain;

namespace AoCEditor.Core.Diagnostics.Application;

/// <summary>What the UI reads from diagnostics: the live log, recent requests, the redactor and debug mode.</summary>
public sealed record DiagnosticsView(BufferLoggerProvider Buffer, GatewayRequestHistory Requests, Redactor Redactor, DebugMode Debug);
