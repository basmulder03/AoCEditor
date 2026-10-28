using Microsoft.Extensions.Logging;

namespace Diagnostics.Application;

public interface ILogSink
{
    void Write(LogEntry entry);
}
