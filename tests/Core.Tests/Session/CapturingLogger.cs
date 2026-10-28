using AoCEditor.Core.Session.Application;
using Microsoft.Extensions.Time.Testing;
using Shared.Contracts;
using Shouldly;
using Xunit;

namespace AoCEditor.Core.Tests.Session;

internal sealed class CapturingLogger<T> : Microsoft.Extensions.Logging.ILogger<T>
{
    public List<string> Lines { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;

    public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
        Lines.Add($"{logLevel} {formatter(state, exception)}");
}
