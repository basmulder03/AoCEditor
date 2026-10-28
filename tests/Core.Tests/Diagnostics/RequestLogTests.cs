using AoCEditor.Core.Diagnostics.Application;
using Shared.Contracts;
using Shouldly;
using Xunit;

namespace AoCEditor.Core.Tests.Diagnostics;

public sealed class RequestLogTests
{
    private static GatewayRequestEntry Entry(string path) => new(DateTimeOffset.UnixEpoch, "GET", path, 200, TimeSpan.FromMilliseconds(12), TimeSpan.Zero);

    [Fact]
    public void KeepsNewestFirstAndBounded()
    {
        var log = new RequestLog(2);

        log.Record(Entry("/1"));
        log.Record(Entry("/2"));
        log.Record(Entry("/3"));

        log.Recent.Select(e => e.Path).ToArray().ShouldBe(["/3", "/2"]);
    }

    [Fact]
    public void RaisesChanged()
    {
        var log = new RequestLog(2);
        var raised = 0;
        log.Changed += (_, _) => raised++;

        log.Record(Entry("/1"));

        raised.ShouldBe(1);
    }
}
