using AoCEditor.Core.Sync.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Contracts;

namespace AoCEditor.Core.Sync.Application;

public enum SyncError
{
    ParseError,
    NotFound,
    NetworkError,
}
