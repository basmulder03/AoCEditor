using AoCEditor.Core.Diagnostics.Application;
using AoCEditor;
using AoCEditor.Infrastructure.Storage;
using Microsoft.Extensions.DependencyInjection;
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
    private ServiceProvider? _services;

    public string? Report { get; private set; }

    public bool DebugIsOn => View.Debug.IsEnabled;

    public IReadOnlyList<string> Messages => [.. View.Buffer.Snapshot().Select(e => e.Message)];

    public string LogFile => File.ReadAllText(Path.Combine(_root, "logs", "aoceditor.log"));

    private ServiceProvider Services => _services ??= Start();

    private DiagnosticsView View => Services.GetRequiredService<DiagnosticsView>();

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
        await View.Debug.SetAsync(true, CancellationToken.None);
    }

    public async Task RestartAsync()
    {
        _services?.Dispose();
        _services = null;
        _mode = DebugMode.FromLaunch([], _ => null);
        await View.Debug.LoadAsync(_store, CancellationToken.None);
    }

    public void RecordDetailedMessage() => Log(LogLevel.Debug, "detailed step");

    public void RecordCookie() => Log(LogLevel.Information, $"request carried session={Cookie} today");

    public void RecordHomeFile() => Log(LogLevel.Information, $"opened {Home}/notes/day1.txt");

    public async Task AskForDiagnosticsAsync()
    {
        var snapshot = new DiagnosticsSnapshot("0.0.0", "TestOS", ".NET", "Signed out", new StorageStatus("db", 1, 1), [], View.Buffer.Snapshot());
        Report = DiagnosticsReport.Build(snapshot, View.Redactor);
        await Task.CompletedTask;
    }

    private ServiceProvider Start()
    {
        var provider = new ServiceCollection()
            .AddSingleton(TimeProvider.System)
            .AddSingleton<IAppDataPaths>(new AppDataPaths(_root))
            .AddDiagnostics(_mode, Home)
            .BuildServiceProvider();
        provider.GetRequiredService<DiagnosticsView>().Debug.LoadAsync(_store, CancellationToken.None).GetAwaiter().GetResult();
        return provider;
    }

    private void Log(LogLevel level, string message) =>
        Services.GetRequiredService<ILoggerFactory>().CreateLogger("Gateway.AocGateway").Log(level, default, message, null, static (s, _) => s);

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
