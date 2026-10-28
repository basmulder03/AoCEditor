using Shouldly;
using Storage.Infrastructure;
using Xunit;

namespace Storage.Tests;

public class AppDataPathsTests
{
    [Fact]
    public void DefaultRootIsThePerUserDataFolder()
    {
        var root = AppDataPaths.DefaultRoot();

        root.ShouldBe(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AoCEditor"));
    }

    [Fact]
    public void FoldersAreCreatedOnlyWhenAskedFor()
    {
        using var temp = new TempDir();
        var paths = new AppDataPaths(Path.Combine(temp.Path, "data"));

        Directory.Exists(temp.Path).ShouldBeFalse();
        var cache = paths.Cache;

        Directory.Exists(cache).ShouldBeTrue();
        cache.ShouldBe(Path.Combine(paths.Root, "cache"));
        paths.Database.ShouldBe(Path.Combine(paths.Root, "aoceditor.db"));
    }
}
