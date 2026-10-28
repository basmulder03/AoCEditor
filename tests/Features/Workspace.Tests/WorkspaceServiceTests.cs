using Shared.Contracts;
using Shouldly;
using Workspace.Application;
using Xunit;

namespace Workspace.Tests;

public sealed class WorkspaceServiceTests : IDisposable
{
    private readonly string _temp = Path.Combine(Path.GetTempPath(), "aoc-ws-" + Guid.NewGuid().ToString("N"));
    private readonly Settings _settings = new();
    private readonly WorkspaceService _service;

    public WorkspaceServiceTests() => _service = new WorkspaceService(_settings, new Paths(Path.Combine(_temp, "appdata")));

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private string Solutions => Path.Combine(_temp, "solutions");

    public void Dispose()
    {
        if (Directory.Exists(_temp))
        {
            Directory.Delete(_temp, recursive: true);
        }
    }

    [Fact]
    public async Task NoRootUntilTheUserPicksOne()
    {
        (await _service.RootAsync(Ct)).ShouldBeNull();
    }

    [Fact]
    public async Task ThePickedRootIsCreatedAndRemembered()
    {
        (await _service.SetRootAsync(Solutions, Ct)).ShouldBe(SetRootResult.Ok);

        (await _service.RootAsync(Ct)).ShouldBe(Solutions);
        Directory.Exists(Solutions).ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("relative/path")]
    public async Task OnlyAbsolutePathsAreAccepted(string path)
    {
        (await _service.SetRootAsync(path, Ct)).ShouldBe(SetRootResult.NotAbsolute);
        (await _service.RootAsync(Ct)).ShouldBeNull();
    }

    [Theory]
    [InlineData("appdata")]
    [InlineData("appdata/inside")]
    [InlineData(".")]
    public async Task ARootOverlappingTheAppDataFolderIsRefused(string sub)
    {
        var path = Path.GetFullPath(Path.Combine(_temp, sub));

        (await _service.SetRootAsync(path, Ct)).ShouldBe(SetRootResult.OverlapsAppData);
        (await _service.RootAsync(Ct)).ShouldBeNull();
    }

    [Fact]
    public async Task ScaffoldingCreatesTheDayFolderWithTheTemplateFiles()
    {
        await _service.SetRootAsync(Solutions, Ct);

        var created = await _service.ScaffoldDayAsync(2024, 3, "python", new Dictionary<string, string> { ["main.py"] = "print(1)\n" }, Ct);

        var file = Path.Combine(Solutions, "2024", "03", "python", "main.py");
        created.ShouldBe([file]);
        File.ReadAllText(file).ShouldBe("print(1)\n");
    }

    [Fact]
    public async Task ScaffoldingNeverOverwritesTheUsersFiles()
    {
        await _service.SetRootAsync(Solutions, Ct);
        var template = new Dictionary<string, string> { ["main.py"] = "template", ["extra.py"] = "extra" };
        await _service.ScaffoldDayAsync(2024, 3, "python", new Dictionary<string, string> { ["main.py"] = "mine" }, Ct);

        var created = await _service.ScaffoldDayAsync(2024, 3, "python", template, Ct);

        created.Select(Path.GetFileName).ShouldBe(["extra.py"]);
        File.ReadAllText(Path.Combine(Solutions, "2024", "03", "python", "main.py")).ShouldBe("mine");
    }

    [Fact]
    public async Task ScaffoldingNeedsARootAndPlainFileNames()
    {
        await Should.ThrowAsync<InvalidOperationException>(() => _service.ScaffoldDayAsync(2024, 3, "python", new Dictionary<string, string>(), Ct));

        await _service.SetRootAsync(Solutions, Ct);
        await Should.ThrowAsync<ArgumentException>(() =>
            _service.ScaffoldDayAsync(2024, 3, "python", new Dictionary<string, string> { ["../evil.py"] = "x" }, Ct));
        File.Exists(Path.Combine(Solutions, "2024", "evil.py")).ShouldBeFalse();
    }

    [Fact]
    public async Task LanguagesWithASolutionForTheDayAreListed()
    {
        await _service.SetRootAsync(Solutions, Ct);
        await _service.ScaffoldDayAsync(2024, 3, "python", new Dictionary<string, string> { ["a"] = "" }, Ct);
        await _service.ScaffoldDayAsync(2024, 3, "dotnet", new Dictionary<string, string> { ["a"] = "" }, Ct);

        (await _service.LanguagesAsync(2024, 3, Ct)).ShouldBe(["dotnet", "python"]);
        (await _service.LanguagesAsync(2024, 4, Ct)).ShouldBeEmpty();
    }

    private sealed class Settings : IKeyValueStore
    {
        private readonly Dictionary<string, string> _values = [];

        public Task<string?> GetAsync(string key, CancellationToken cancellationToken) =>
            Task.FromResult(_values.TryGetValue(key, out var v) ? v : null);

        public Task SetAsync(string key, string value, CancellationToken cancellationToken)
        {
            _values[key] = value;
            return Task.CompletedTask;
        }
    }

    private sealed class Paths(string root) : IAppDataPaths
    {
        public string Root => root;

        public string Cache => Path.Combine(root, "cache");

        public string Database => Path.Combine(root, "aoc.db");
    }

    [Theory]
    [InlineData("C:evil.cs")]
    [InlineData("name:stream")]
    [InlineData("CON")]
    [InlineData("aux.txt")]
    [InlineData("../escape.py")]
    public async Task ScaffoldingRefusesUnsafeNames(string name)
    {
        await _service.SetRootAsync(Solutions, Ct);

        await Should.ThrowAsync<ArgumentException>(() => _service.ScaffoldDayAsync(2024, 3, "python", new Dictionary<string, string> { [name] = "x" }, Ct));
    }
}
