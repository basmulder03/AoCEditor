using AoCEditor.Core.Workspace.Application;
using AoCEditor.Infrastructure.Workspace;
using Shared.Contracts;

namespace Specs.Drivers.Workspace;

public sealed class ScaffoldingDriver : IDisposable
{
    private const string Template = "print(input())\n";

    private readonly string _temp = Path.Combine(Path.GetTempPath(), "aoc-spec-" + Guid.NewGuid().ToString("N"));
    private readonly Dictionary<string, string> _settings = [];
    private readonly WorkspaceService _service;

    public ScaffoldingDriver() => _service = new WorkspaceService(new Settings(_settings), new Paths(Path.Combine(_temp, "appdata")));

    public IReadOnlyList<string> Created { get; private set; } = [];

    public SetRootResult RootResult { get; private set; }

    public string SolutionFile(int year, int day, string language) =>
        Path.Combine(_temp, "solutions", year.ToString("0000", System.Globalization.CultureInfo.InvariantCulture), day.ToString("00", System.Globalization.CultureInfo.InvariantCulture), language, "main.py");

    public async Task ChooseSolutionsFolderAsync() => RootResult = await _service.SetRootAsync(Path.Combine(_temp, "solutions"), CancellationToken.None);

    public async Task ChooseAppDataAsSolutionsFolderAsync() => RootResult = await _service.SetRootAsync(Path.Combine(_temp, "appdata"), CancellationToken.None);

    public async Task StartDayAsync(int year, int day, string language) =>
        Created = await _service.ScaffoldDayAsync(year, day, language, new Dictionary<string, string> { ["main.py"] = Template }, CancellationToken.None);

    public void EditSolution(int year, int day, string language) => File.WriteAllText(SolutionFile(year, day, language), "my work\n");

    public string ReadSolution(int year, int day, string language) => File.ReadAllText(SolutionFile(year, day, language));

    public void Dispose()
    {
        if (Directory.Exists(_temp))
        {
            Directory.Delete(_temp, recursive: true);
        }
    }

    private sealed class Settings(Dictionary<string, string> values) : IKeyValueStore
    {
        public Task<string?> GetAsync(string key, CancellationToken cancellationToken) =>
            Task.FromResult(values.TryGetValue(key, out var v) ? v : null);

        public Task SetAsync(string key, string value, CancellationToken cancellationToken)
        {
            values[key] = value;
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
