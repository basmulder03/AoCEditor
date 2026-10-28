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
