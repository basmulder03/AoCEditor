namespace Session.Application;

public sealed record SessionStatus(SessionState State, string? Reason = null);
