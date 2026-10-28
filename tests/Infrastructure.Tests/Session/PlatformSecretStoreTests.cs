using AoCEditor.Core.Session.Application;
using AoCEditor.Infrastructure.Session;
using Shouldly;
using Xunit;

namespace AoCEditor.Infrastructure.Tests.Session;

[Trait("Category", "Integration")]
public sealed class PlatformSecretStoreTests
{
    private static readonly ISecretStore Store = SecretStores.CreateForCurrentOs();

    public static bool ServiceAvailable { get; } = Probe();

    private static bool Probe() =>
        Store.GetAsync("aoceditor-probe", CancellationToken.None).GetAwaiter().GetResult() is not SecretResult.Unavailable;

    [Fact(SkipUnless = nameof(ServiceAvailable), SkipType = typeof(PlatformSecretStoreTests), Skip = "No secret service available.")]
    public async Task SetGetDeleteRoundTrip()
    {
        var name = "aoceditor-test-" + Guid.NewGuid().ToString("N");
        var ct = TestContext.Current.CancellationToken;
        try
        {
            (await Store.GetAsync(name, ct)).ShouldBeOfType<SecretResult.NotFound>();
            (await Store.SetAsync(name, "synthetic-value ü", ct)).ShouldBeOfType<SecretResult.Ok>();
            (await Store.GetAsync(name, ct)).ShouldBeOfType<SecretResult.Ok>().Value.ShouldBe("synthetic-value ü");
            (await Store.SetAsync(name, "second", ct)).ShouldBeOfType<SecretResult.Ok>();
            (await Store.GetAsync(name, ct)).ShouldBeOfType<SecretResult.Ok>().Value.ShouldBe("second");
            (await Store.DeleteAsync(name, ct)).ShouldBeOfType<SecretResult.Ok>();
            (await Store.DeleteAsync(name, ct)).ShouldBeOfType<SecretResult.NotFound>();
        }
        finally
        {
            await Store.DeleteAsync(name, ct);
        }
    }
}
