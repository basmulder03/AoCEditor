using Microsoft.Extensions.Time.Testing;
using Session.Application;
using Shared.Contracts;
using Shouldly;
using Xunit;

namespace Session.Tests;

internal sealed class FakeGateway : IAocGateway
{
    public GatewayResult Answer { get; set; } = new GatewayResult.Ok("page");

    public Queue<GatewayResult> Answers { get; } = new();

    public List<string> Checked { get; } = [];

    public Task<GatewayResult> CheckTokenAsync(string token, CancellationToken cancellationToken)
    {
        Checked.Add(token);
        return Task.FromResult(Answers.TryDequeue(out var next) ? next : Answer);
    }

    public Task<GatewayResult> GetAsync(string path, RequestKind kind, FetchPolicy policy, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<GatewayResult> PostFormAsync(string path, IReadOnlyDictionary<string, string> form, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public DateTimeOffset NextAllowedAt(RequestKind kind) => DateTimeOffset.UnixEpoch;
}
