using AoCEditor;
using Microsoft.Extensions.DependencyInjection;

namespace Ui.Tests;

/// <summary>The real registrations with machine-specific parts replaced; disposing it disposes the services as the app does on exit.</summary>
internal sealed class TestApp : IDisposable
{
    private readonly ServiceProvider _provider;

    public TestApp(AppOverrides overrides) =>
        _provider = new ServiceCollection().AddAocEditor(overrides).BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

    public AppServices Services => _provider.GetRequiredService<AppServices>();

    public static implicit operator AppServices(TestApp app) => app.Services;

    public void Dispose() => _provider.Dispose();
}
