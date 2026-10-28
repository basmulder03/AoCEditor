using Diagnostics.Application;
using Shared.Contracts;

namespace Diagnostics.Tests;

internal sealed class ListSink : ILogSink
{
    public List<LogEntry> Entries { get; } = [];

    public void Write(LogEntry entry) => Entries.Add(entry);
}
