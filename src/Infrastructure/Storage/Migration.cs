using System.Globalization;
using System.Reflection;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AoCEditor.Infrastructure.Storage;

public sealed record Migration(int Version, string Sql);
