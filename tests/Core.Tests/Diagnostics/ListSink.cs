using AoCEditor.Core.Diagnostics.Application;
using Shared.Contracts;

namespace AoCEditor.Core.Tests.Diagnostics;

internal sealed class ListSink : ILogSink
{
    public List<LogEntry> Entries { get; } = [];

    public void Write(LogEntry entry) => Entries.Add(entry);
}
