namespace AoCEditor.Core.Session.Application;

public sealed record SessionStatus(SessionState State, string? Reason = null);
