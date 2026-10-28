using AoCEditor.Core.Sync.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Contracts;

namespace AoCEditor.Core.Sync.Application;

public sealed record YearError(int Year, SyncError Error, string? Reason = null);
