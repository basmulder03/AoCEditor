using AoCEditor;
using Xunit;
using Plugins.Domain;
using Puzzle.Application;
using Runner.Application;
using Shared.Contracts;
using Shouldly;
using Workspace.Application;

namespace Ui.Tests;

public sealed class DayRunCoordinatorTests : IDisposable
{
    private readonly string _temp = Path.Combine(Path.GetTempPath(), "aoc-coord-" + Guid.NewGuid().ToString("N"));
    private readonly Settings _settings = new();
    private readonly WorkspaceService _workspace;
    private readonly FakeRunner _runner = new();
    private readonly Paths _paths;
    private Fetched<string> _input = new(FetchStatus.Ok, "1 2 3\n");
    private string? _toolchain = "/usr/bin/tool";

    public DayRunCoordinatorTests()
    {
        _paths = new Paths(Path.Combine(_temp, "appdata"));
        _workspace = new WorkspaceService(_settings, _paths);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static PluginManifest Python => ManifestParser.Parse("""
        { "schemaVersion": 1, "id": "python", "name": "Python", "extensions": [".py"],
          "templateFiles": { "main.py": "print(1)" },
          "toolchain": { "executables": ["python3"] },
          "run": { "command": ["{toolchain}", "{source}"], "input": "stdin" } }
        """).Manifest!;

    private static PluginManifest Compiled => ManifestParser.Parse("""
        { "schemaVersion": 1, "id": "dotnet", "name": "NET", "extensions": [".cs"],
          "templateFiles": { "Program.cs": "x" },
          "toolchain": { "executables": ["dotnet"] },
          "compile": { "command": ["{toolchain}", "build", "{workdir}"] },
          "run": { "command": ["{toolchain}", "run", "--input", "{input}"], "input": "file" } }
        """).Manifest!;

    private string Solutions => Path.Combine(_temp, "solutions");

    private DayRunCoordinator Create() =>
        new(_workspace, (_, _, _) => Task.FromResult(_input), _runner, _paths, _ => _toolchain);

    public void Dispose()
    {
        if (Directory.Exists(_temp))
        {
            Directory.Delete(_temp, recursive: true);
        }
    }

    [Fact]
    public async Task WithoutAWorkspaceNothingRuns()
    {
        var result = await Create().RunAsync(2024, 3, Python, new Progress<OutputLine>(), allowUnsandboxed: false, Ct);

        result.Kind.ShouldBe(DayRunKind.NoWorkspace);
        _runner.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task AMissingToolchainNamesWhatToInstall()
    {
        await _workspace.SetRootAsync(Solutions, Ct);
        _toolchain = null;

        var result = await Create().RunAsync(2024, 3, Python, new Progress<OutputLine>(), false, Ct);

        result.Kind.ShouldBe(DayRunKind.NoToolchain);
        result.Detail!.ShouldContain("python3");
    }

    [Fact]
    public async Task ARunScaffoldsTheDayStoresTheInputInAppDataAndPipesIt()
    {
        await _workspace.SetRootAsync(Solutions, Ct);
        _runner.Next = new RunResult(RunStatus.Completed, 0, TimeSpan.FromMilliseconds(5));

        var result = await Create().RunAsync(2024, 3, Python, new Progress<OutputLine>(), false, Ct);

        result.Kind.ShouldBe(DayRunKind.Ran);
        var folder = Path.Combine(Solutions, "2024", "03", "python");
        var request = _runner.Requests.Single();
        request.Command.ShouldBe(["/usr/bin/tool", Path.Combine(folder, "main.py")]);
        request.WorkingDirectory.ShouldBe(folder);
        request.InputPath!.ShouldStartWith(_paths.Root);
        request.InputPath.ShouldNotStartWith(Solutions);
        File.ReadAllText(request.InputPath).ShouldBe("1 2 3\n");
        File.Exists(Path.Combine(folder, "main.py")).ShouldBeTrue();
        Directory.EnumerateFiles(Solutions, "*", SearchOption.AllDirectories).ShouldAllBe(f => !f.EndsWith("3.txt", StringComparison.Ordinal));
    }

    [Fact]
    public async Task FileInputPassesThePathAsAPlaceholderInsteadOfStdin()
    {
        await _workspace.SetRootAsync(Solutions, Ct);
        _runner.Next = new RunResult(RunStatus.Completed, 0, TimeSpan.Zero);

        await Create().RunAsync(2024, 3, Compiled, new Progress<OutputLine>(), false, Ct);

        var run = _runner.Requests[1];
        run.InputPath.ShouldBeNull();
        run.Command[^1].ShouldEndWith(Path.Combine("inputs", "2024", "03.txt"));
    }

    [Fact]
    public async Task AFailedCompileStopsBeforeTheRun()
    {
        await _workspace.SetRootAsync(Solutions, Ct);
        _runner.Next = new RunResult(RunStatus.Completed, 1, TimeSpan.Zero);

        var result = await Create().RunAsync(2024, 3, Compiled, new Progress<OutputLine>(), false, Ct);

        result.Kind.ShouldBe(DayRunKind.CompileFailed);
        _runner.Requests.Count.ShouldBe(1);
    }

    [Fact]
    public async Task AnUnavailableInputStopsTheRun()
    {
        await _workspace.SetRootAsync(Solutions, Ct);
        _input = new Fetched<string>(FetchStatus.NotSignedIn);

        var result = await Create().RunAsync(2024, 3, Python, new Progress<OutputLine>(), false, Ct);

        result.Kind.ShouldBe(DayRunKind.InputUnavailable);
        _runner.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task ARefusedSandboxIsReportedSoTheUserCanBeAsked()
    {
        await _workspace.SetRootAsync(Solutions, Ct);
        _runner.Next = new RunResult(RunStatus.SandboxUnavailable, null, TimeSpan.Zero, "no sandbox");

        var result = await Create().RunAsync(2024, 3, Python, new Progress<OutputLine>(), false, Ct);

        result.Kind.ShouldBe(DayRunKind.SandboxRefused);
    }

    [Fact]
    public async Task OnlyTheBuildStepMayUseTheNetworkAndWriteItsFolder()
    {
        await _workspace.SetRootAsync(Solutions, Ct);

        await Create().RunAsync(2024, 3, Compiled, new Progress<OutputLine>(), false, Ct);

        var folder = Path.Combine(Solutions, "2024", "03", "dotnet");
        var input = Path.Combine(_paths.Root, "inputs", "2024", "03.txt");
        var (build, run) = (_runner.Requests[0], _runner.Requests[1]);
        build.AllowNetwork.ShouldBeTrue();
        build.WritablePaths.ShouldBe([folder]);
        run.AllowNetwork.ShouldBeFalse();
        run.WritablePaths.ShouldBeNull();
        run.ReadablePaths.ShouldBe([input]);
    }

    [Fact]
    public async Task ARefusedSandboxPassesOnWhatIsMissing()
    {
        await _workspace.SetRootAsync(Solutions, Ct);
        _runner.Next = new RunResult(RunStatus.SandboxUnavailable, null, TimeSpan.Zero, "network isolation is missing");

        var result = await Create().RunAsync(2024, 3, Python, new Progress<OutputLine>(), false, Ct);

        result.Detail.ShouldBe("network isolation is missing");
    }

    [Fact]
    public async Task ScaffoldingWithoutAWorkspaceIsAResultNotAnException()
    {
        var result = await Create().ScaffoldAsync(2024, 3, Python, Ct);

        result.Kind.ShouldBe(DayScaffoldKind.NoWorkspace);
    }

    [Fact]
    public async Task ScaffoldingCreatesTheSourceWithoutRunningOrLoadingInput()
    {
        await _workspace.SetRootAsync(Solutions, Ct);
        _input = new Fetched<string>(FetchStatus.NotSignedIn);

        var result = await Create().ScaffoldAsync(2024, 3, Python, Ct);

        var folder = Path.Combine(Solutions, "2024", "03", "python");
        result.Kind.ShouldBe(DayScaffoldKind.Scaffolded);
        result.Folder.ShouldBe(folder);
        result.SourcePath.ShouldBe(Path.Combine(folder, "main.py"));
        File.ReadAllText(result.SourcePath!).ShouldBe("print(1)");
        _runner.Requests.ShouldBeEmpty();
    }

    private sealed class FakeRunner : IRunner
    {
        public SandboxLevel Level => SandboxLevel.None;

        public List<RunRequest> Requests { get; } = [];

        public RunResult Next { get; set; } = new(RunStatus.Completed, 0, TimeSpan.Zero);

        public Task<RunResult> RunAsync(RunRequest request, IProgress<OutputLine> output, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(Next);
        }
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
}
