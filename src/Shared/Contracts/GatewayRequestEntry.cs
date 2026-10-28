namespace Shared.Contracts;

/// <summary>One gateway request as shown in the debug view: never headers, bodies, queries or tokens.</summary>
public sealed record GatewayRequestEntry(DateTimeOffset At, string Method, string Path, int? Status, TimeSpan Duration, TimeSpan ThrottleWait);
