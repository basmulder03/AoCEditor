using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Contracts;

namespace AoCEditor.Infrastructure.Storage;

public interface IConnectionFactory
{
    /// <summary>Opens a migrated connection with WAL and foreign keys on.</summary>
    Task<SqliteConnection> OpenAsync(CancellationToken cancellationToken);
}
