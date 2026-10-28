using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Contracts;
using Sync.Domain;

namespace Sync.Application;

public enum SyncOutcome
{
    Completed,
    NotSignedIn,
    RateLimited,
    EventsUnavailable,
}
