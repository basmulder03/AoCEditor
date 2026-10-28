using Microsoft.Extensions.Logging;

namespace AoCEditor.Core.Diagnostics.Application;

public interface ILogSink
{
    void Write(LogEntry entry);
}
