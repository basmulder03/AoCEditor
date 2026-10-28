using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Contracts;
using Sync.Domain;

namespace Sync.Application;

/// <summary>Reported just before a year's request: the year in progress, years queued after it and the wait before the request may go out.</summary>
public sealed record SyncProgress(int Year, int YearsRemaining, int SecondsUntilNextRequest);
