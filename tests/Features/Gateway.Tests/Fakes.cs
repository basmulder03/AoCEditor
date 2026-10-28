using System.Net;
using Gateway;
using Shared.Contracts;

namespace Gateway.Tests;

internal sealed class FakeHandler : HttpMessageHandler
{
    public List<HttpRequestMessage> Requests { get; } = [];

    public Func<HttpRequestMessage, HttpResponseMessage> Respond { get; set; } = _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("body") };

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        return Task.FromResult(Respond(request));
    }
}

internal sealed class FakeTokens(string? token) : ISessionTokenProvider
{
    public ValueTask<string?> GetAsync(CancellationToken cancellationToken) => ValueTask.FromResult(token);
}

internal sealed class MemoryCache : IResponseCache
{
    public Dictionary<string, string> Items { get; } = [];

    public ValueTask<string?> TryGetAsync(string path, CancellationToken cancellationToken) =>
        ValueTask.FromResult(Items.TryGetValue(path, out var v) ? v : null);

    public ValueTask SetAsync(string path, string content, CancellationToken cancellationToken)
    {
        Items[path] = content;
        return ValueTask.CompletedTask;
    }
}

internal sealed class CapturingLogger : Microsoft.Extensions.Logging.ILogger
{
    public List<string> Lines { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;

    public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
        Lines.Add($"{logLevel} {formatter(state, exception)}");
}

internal sealed class ListRequestLog : IRequestLog
{
    private readonly List<GatewayRequestEntry> _all = [];

    public IReadOnlyList<GatewayRequestEntry> Recent => _all;

    public event EventHandler? Changed;

    public void Record(GatewayRequestEntry entry)
    {
        _all.Add(entry);
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
