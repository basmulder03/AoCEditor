using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Contracts;
using Sync.Domain;

namespace Sync.Application;

public sealed record YearError(int Year, SyncError Error, string? Reason = null);
