using Plugins.Application;
using Shouldly;
using Xunit;

namespace Plugins.Tests;

public class BuiltInPluginsTests
{
    [Fact]
    public void PythonAndDotnetShipAsValidManifests()
    {
        var manifests = BuiltInPlugins.Load();

        manifests.Select(m => m.Id).ShouldBe(["dotnet", "python"]);
    }

    [Fact]
    public void EveryBuiltInTemplateIsNonEmpty()
    {
        BuiltInPlugins.Load().ShouldAllBe(m => m.TemplateFiles.Count > 0 && m.TemplateFiles.Values.All(c => c.Length > 0));
    }
}
