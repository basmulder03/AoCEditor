using AoCEditor.Infrastructure.Plugins;
using Shouldly;
using Xunit;

namespace AoCEditor.Infrastructure.Tests.Plugins;

public sealed class ToolchainLocatorTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "aoc-tc-" + Guid.NewGuid().ToString("N"));

    public ToolchainLocatorTests()
    {
        Directory.CreateDirectory(Path.Combine(_root, "a"));
        Directory.CreateDirectory(Path.Combine(_root, "b"));
    }

    public static bool RunsHere => !OperatingSystem.IsWindows();

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private string Make(string dir, string name, bool executable = true)
    {
        var path = Path.Combine(_root, dir, name);
        File.WriteAllText(path, "#!/bin/sh\n");
        if (executable && !OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserExecute);
        }

        return path;
    }

    private string PathOf(params string[] dirs) => string.Join(Path.PathSeparator, dirs.Select(d => Path.Combine(_root, d)));

    [Fact(Skip = "Unix permissions", SkipUnless = nameof(RunsHere), SkipType = typeof(ToolchainLocatorTests))]
    public void TheFirstExecutableNameFoundOnPathWins()
    {
        var python = Make("b", "python");
        Make("a", "python3", executable: true);

        // names are tried in the manifest's order, directories in PATH order
        ToolchainLocator.Find(["python", "python3"], PathOf("a", "b")).ShouldBe(python);
    }

    [Fact(Skip = "Unix permissions", SkipUnless = nameof(RunsHere), SkipType = typeof(ToolchainLocatorTests))]
    public void AFileWithoutTheExecuteBitIsNotAToolchain()
    {
        Make("a", "python3", executable: false);

        ToolchainLocator.Find(["python3"], PathOf("a")).ShouldBeNull();
    }

    [Fact]
    public void NothingFoundIsNull()
    {
        ToolchainLocator.Find(["nope-aoc"], PathOf("a", "b")).ShouldBeNull();
        ToolchainLocator.Find(["x"], null).ShouldBeNull();
    }

    [Fact]
    public void NamesWithAPathAreNeverSearched()
    {
        ToolchainLocator.Find(["../a/python3"], PathOf("a")).ShouldBeNull();
    }

    [Fact(Skip = "Unix permissions", SkipUnless = nameof(RunsHere), SkipType = typeof(ToolchainLocatorTests))]
    public void RelativePathEntriesAreIgnored()
    {
        Make("a", "python3");
        var previous = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(_root);
        try
        {
            ToolchainLocator.Find(["python3"], "a").ShouldBeNull();
        }
        finally
        {
            Directory.SetCurrentDirectory(previous);
        }
    }
}
