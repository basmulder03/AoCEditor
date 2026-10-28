using Shared.Contracts;

namespace AoCEditor.Core.Diagnostics.Application;

/// <summary>Debug mode from the --debug flag, the AOCEDITOR_DEBUG variable or a persisted setting.</summary>
public sealed class DebugMode
{
    public const string EnvironmentVariable = "AOCEDITOR_DEBUG";
    public const string SettingKey = "diagnostics.debug";

    private IKeyValueStore? _store;
    private volatile bool _persisted;

    private DebugMode(bool forced) => IsForced = forced;

    public event EventHandler? Changed;

    public bool IsForced { get; }

    public bool IsPersisted => _persisted;

    public bool IsEnabled => IsForced || _persisted;

    public static DebugMode FromLaunch(IEnumerable<string> args, Func<string, string?> environment)
    {
        var env = environment(EnvironmentVariable);
        var forced = args.Contains("--debug", StringComparer.Ordinal)
            || env is "1" || string.Equals(env, "true", StringComparison.OrdinalIgnoreCase);
        return new DebugMode(forced);
    }

    public void AttachStore(IKeyValueStore store) => _store = store;

    public async Task LoadAsync(IKeyValueStore store, CancellationToken cancellationToken)
    {
        _store = store;
        var value = await store.GetAsync(SettingKey, cancellationToken);
        Apply(string.Equals(value, "true", StringComparison.OrdinalIgnoreCase));
    }

    public async Task SetAsync(bool enabled, CancellationToken cancellationToken)
    {
        if (_store is not null)
        {
            await _store.SetAsync(SettingKey, enabled ? "true" : "false", cancellationToken);
        }

        Apply(enabled);
    }

    private void Apply(bool persisted)
    {
        var before = IsEnabled;
        _persisted = persisted;
        if (before != IsEnabled || persisted)
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}
