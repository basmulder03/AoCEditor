using AoCEditor.Core.Diagnostics.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AoCEditor.Infrastructure.Diagnostics;

public static class ConsoleLogging
{
    /// <summary>The built-in single-line console provider (stderr); it writes only while debug mode is on.</summary>
    public static ILoggerProvider CreateProvider(DebugMode debug)
    {
        var services = new ServiceCollection().AddLogging(b => b
            .SetMinimumLevel(LogLevel.Trace)
            .AddFilter((_, _) => debug.IsEnabled)
            .AddSimpleConsole(o =>
            {
                o.SingleLine = true;
                o.TimestampFormat = "HH:mm:ss.fff ";
            })
            .AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace)).BuildServiceProvider();
        return new OwnedProvider(services);
    }

    // Loggers come from the private factory so its debug-mode filter applies.
    private sealed class OwnedProvider(ServiceProvider services) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => services.GetRequiredService<ILoggerFactory>().CreateLogger(categoryName);

        public void Dispose() => services.Dispose();
    }
}
