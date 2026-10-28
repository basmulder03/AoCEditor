using AoCEditor.Infrastructure.Gateway;
using Shouldly;
using Xunit;

namespace AoCEditor.Infrastructure.Tests.Gateway;

public sealed class FileResponseCacheTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }

    [Fact]
    public async Task MissingEntryIsNull()
    {
        var cache = new FileResponseCache(_dir);

        var value = await cache.TryGetAsync("/2024/day/1/input", TestContext.Current.CancellationToken);

        value.ShouldBeNull();
    }

    [Fact]
    public async Task StoredEntryIsReadBackAndOverwrittenWithoutLeftovers()
    {
        var cache = new FileResponseCache(_dir);
        var ct = TestContext.Current.CancellationToken;

        await cache.SetAsync("/2024/day/1/input", "first", ct);
        await cache.SetAsync("/2024/day/1/input", "second", ct);

        (await cache.TryGetAsync("/2024/day/1/input", ct)).ShouldBe("second");
        Directory.GetFiles(_dir).Length.ShouldBe(1);
    }

    [Fact]
    public async Task DifferentPathsDoNotCollide()
    {
        var cache = new FileResponseCache(_dir);
        var ct = TestContext.Current.CancellationToken;

        await cache.SetAsync("/a?x=1", "one", ct);
        await cache.SetAsync("/a?x=2", "two", ct);

        (await cache.TryGetAsync("/a?x=1", ct)).ShouldBe("one");
    }
}
