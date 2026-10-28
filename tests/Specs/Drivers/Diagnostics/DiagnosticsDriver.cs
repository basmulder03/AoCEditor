using AoCEditor.Core.Diagnostics.Application;
using AoCEditor.Infrastructure.Diagnostics;
using Microsoft.Extensions.Logging;
using Shared.Contracts;

namespace Specs.Drivers.Diagnostics;

public sealed class DiagnosticsDriver : IDisposable
{
    public const string Cookie = "synthetic-cookie-value-123";
    public const string Home = "/home/synthetic";
    private readonly string _root = Path.Combine(Path.GetTempPath(), "aoc-spec-" + Guid.NewGuid().ToString("N"));
    private readonly Store _store = new();
    private DebugMode _mode = DebugMode.FromLaunch([], _ => null);
    private DiagnosticsServices? _services;

    public string? Report { get; private set; }

    public bool DebugIsOn => Services.Debug.IsEnabled;

    public IReadOnlyList<string> Messages => [.. Services.Buffer.Snapshot().Select(e => e.Message)];

    public string LogFile => File.ReadAllText(Path.Combine(_root, "logs", "aoceditor.log"));

    private DiagnosticsServices Services => _services ??= Start();

    public void Dispose()
    {
        _services?.Dispose();
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    public void StartWithDebugOff() => _mode = DebugMode.FromLaunch([], _ => null);

    public void StartWithDebugOn() => _mode = DebugMode.FromLaunch(["--debug"], _ => null);

    public async Task SwitchDebugOnAsync()
    {
        _ = Services;
        await Services.Debug.SetAsync(true, CancellationToken.None);
    }

    public async Task RestartAsync()
    {
        _services?.Dispose();
        _services = null;
        _mode = DebugMode.FromLaunch([], _ => null);
        await Services.Debug.LoadAsync(_store, CancellationToken.None);
    }

    public void RecordDetailedMessage() => Log(LogLevel.Debug, "detailed step");

    public void RecordCookie() => Log(LogLevel.Information, $"request carried session={Cookie} today");

    public void RecordHomeFile() => Log(LogLevel.Information, $"opened {Home}/notes/day1.txt");

    public async Task AskForDiagnosticsAsync()
    {
        var snapshot = new DiagnosticsSnapshot("0.0.0", "TestOS", ".NET", "Signed out", new StorageStatus("db", 1, 1), [], Services.Buffer.Snapshot());
        Report = DiagnosticsReport.Build(snapshot, Services.Redactor);
        await Task.CompletedTask;
    }

    private DiagnosticsServices Start()
    {
        _mode.AttachStore(_store);
        return DiagnosticsServices.Create(_root, _mode, Home, TimeProvider.System, TextWriter.Null);
    }

    private void Log(LogLevel level, string message) =>
        Services.LoggerFactory.CreateLogger("Gateway.AocGateway").Log(level, default, message, null, static (s, _) => s);

    private sealed class Store : IKeyValueStore
    {
        private readonly Dictionary<string, string> _items = [];

        public Task<string?> GetAsync(string key, CancellationToken cancellationToken) => Task.FromResult(_items.TryGetValue(key, out var v) ? v : null);

        public Task SetAsync(string key, string value, CancellationToken cancellationToken)
        {
            _items[key] = value;
            return Task.CompletedTask;
        }
    }
}
