using Microsoft.Extensions.Time.Testing;
using Session.Application;
using Shared.Contracts;
using Shouldly;
using Xunit;

namespace Session.Tests;

internal sealed class CapturingLogger : Microsoft.Extensions.Logging.ILogger
{
    public List<string> Lines { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;

    public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
        Lines.Add($"{logLevel} {formatter(state, exception)}");
}
