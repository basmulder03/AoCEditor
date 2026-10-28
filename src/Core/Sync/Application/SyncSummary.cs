using AoCEditor.Core.Sync.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Contracts;

namespace AoCEditor.Core.Sync.Application;

public sealed record SyncSummary(SyncOutcome Outcome, IReadOnlyList<int> YearsSynced, int DaysUpdated, IReadOnlyList<YearError> Errors, IReadOnlyList<int> YearsSkipped);
