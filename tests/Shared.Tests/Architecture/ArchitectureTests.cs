using Shouldly;
using Xunit;

namespace Shared.Tests.Architecture;

public sealed class ArchitectureTests : IDisposable
{
    private readonly string _temp = Path.Combine(Path.GetTempPath(), "aoc-arch-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_temp))
        {
            Directory.Delete(_temp, recursive: true);
        }
    }

    private static string Src => Path.Combine(SliceScanner.FindRoot(), "src");

    [Fact]
    public void NoSliceReferencesAnotherSlice() =>
        SliceScanner.CrossSliceReferences(Src).ShouldBeEmpty();

    [Fact]
    public void NamespacesMirrorTheFolderLayout() =>
        SliceScanner.NamespacesNotMirroringFolders(Src).ShouldBeEmpty();

    [Fact]
    public void ScannerFlagsAReferenceToAnotherSlice()
    {
        Write("Core/Sync/Domain/A.cs", "using AoCEditor.Core.Puzzle.Domain;\nnamespace AoCEditor.Core.Sync.Domain;\n");
        Write("UI/Sync/B.axaml", "<UserControl xmlns:p=\"using:AoCEditor.UI.Puzzle\" />");
        Write("Infrastructure/Sync/C.cs", "using AoCEditor.Core.Sync.Domain;\nnamespace AoCEditor.Infrastructure.Sync;\n");

        var found = SliceScanner.CrossSliceReferences(Path.Combine(_temp, "src"));

        found.Count.ShouldBe(2);
        found.ShouldContain(f => f.Contains("A.cs", StringComparison.Ordinal));
        found.ShouldContain(f => f.Contains("B.axaml", StringComparison.Ordinal));
    }

    [Fact]
    public void CoreReferencesNeitherAvaloniaNorSqliteNorTheOtherLayers()
    {
        var references = AssemblyReferences.OfLayer("Core");

        references.ShouldContain("Shared");
        references.ShouldNotContain(r => r.StartsWith("Avalonia", StringComparison.Ordinal) || r.StartsWith("Microsoft.Data.Sqlite", StringComparison.Ordinal));
        references.ShouldNotContain("AoCEditor.Infrastructure");
        references.ShouldNotContain("AoCEditor.UI");
    }

    [Fact]
    public void InfrastructureAndUiDoNotReferenceEachOther()
    {
        AssemblyReferences.OfLayer("Infrastructure").ShouldNotContain("AoCEditor.UI");
        AssemblyReferences.OfLayer("UI").ShouldNotContain("AoCEditor.Infrastructure");
    }

    private void Write(string relative, string content)
    {
        var path = Path.Combine(_temp, "src", relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }
}
