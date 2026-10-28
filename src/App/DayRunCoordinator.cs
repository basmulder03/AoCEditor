using System.Globalization;
using Plugins.Domain;
using Puzzle.Application;
using Runner.Application;
using Shared.Contracts;
using Workspace.Application;
using Workspace.Domain;

namespace AoCEditor;

/// <summary>Ties the slices together for one run: scaffold the day, find the toolchain, store the input in app data, compile, run.</summary>
public sealed class DayRunCoordinator(
    WorkspaceService workspace,
    Func<int, int, CancellationToken, Task<Fetched<string>>> loadInput,
    IRunner runner,
    IAppDataPaths appData,
    Func<PluginManifest, string?> findToolchain)
{
    // shortcut: fixed limits, make them settings when the settings page grows a run section.
    private static readonly TimeSpan CompileTimeout = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan RunTimeout = TimeSpan.FromMinutes(1);

    /// <summary>Creates the day's template files (never overwriting) without running anything or needing the input.</summary>
    public async Task<DayScaffoldResult> ScaffoldAsync(int year, int day, PluginManifest plugin, CancellationToken cancellationToken)
    {
        if (await workspace.RootAsync(cancellationToken) is not { } root)
        {
            return new DayScaffoldResult(DayScaffoldKind.NoWorkspace);
        }

        await workspace.ScaffoldDayAsync(year, day, plugin.Id, plugin.TemplateFiles, cancellationToken);
        var folder = WorkspaceLayout.DayFolder(root, year, day, plugin.Id);
        return new DayScaffoldResult(DayScaffoldKind.Scaffolded, folder, Path.Combine(folder, SourceName(plugin)));
    }

    public async Task<DayRunResult> RunAsync(int year, int day, PluginManifest plugin, IProgress<OutputLine> output, bool allowUnsandboxed, CancellationToken cancellationToken)
    {
        if (await workspace.RootAsync(cancellationToken) is not { } root)
        {
            return new DayRunResult(DayRunKind.NoWorkspace, Detail: "Choose a solutions folder first.");
        }

        if (findToolchain(plugin) is not { } toolchain)
        {
            return new DayRunResult(DayRunKind.NoToolchain, Detail: $"{plugin.Name} was not found. Install it and make sure one of these is on your PATH: {string.Join(", ", plugin.ToolchainExecutables)}.");
        }

        var input = await loadInput(year, day, cancellationToken);
        if (input.Value is not { } inputText)
        {
            return new DayRunResult(DayRunKind.InputUnavailable, Detail: "The puzzle input could not be loaded. Sign in and check your connection.");
        }

        await workspace.ScaffoldDayAsync(year, day, plugin.Id, plugin.TemplateFiles, cancellationToken);
        var folder = WorkspaceLayout.DayFolder(root, year, day, plugin.Id);
        var inputPath = await WriteInputAsync(year, day, inputText, cancellationToken);
        var values = new Dictionary<string, string>
        {
            ["toolchain"] = toolchain,
            ["source"] = Path.Combine(folder, SourceName(plugin)),
            ["input"] = inputPath,
            ["workdir"] = folder,
        };

        if (plugin.Compile is { } compile)
        {
            // Package restore needs the network and the build writes into the day folder, so only this step gets both.
            var built = await runner.RunAsync(new RunRequest(CommandTemplate.Expand(compile.Command, values), folder, null, CompileTimeout, allowUnsandboxed, [inputPath], [folder], AllowNetwork: true), output, cancellationToken);
            if (built.Status == RunStatus.SandboxUnavailable)
            {
                return new DayRunResult(DayRunKind.SandboxRefused, built, $"{built.Error} {SandboxLevelExtensions.BuildStepNotice}");
            }

            if (built.Status == RunStatus.SandboxFailed)
            {
                return new DayRunResult(DayRunKind.CompileFailed, built, built.Error);
            }

            if (built.Status != RunStatus.Completed || built.ExitCode != 0)
            {
                return new DayRunResult(DayRunKind.CompileFailed, built, "The build failed; see the console.");
            }
        }

        var piped = plugin.Run.Input == InputMode.Stdin ? inputPath : null;
        var run = await runner.RunAsync(new RunRequest(CommandTemplate.Expand(plugin.Run.Command, values), folder, piped, RunTimeout, allowUnsandboxed, [inputPath]), output, cancellationToken);
        return run.Status == RunStatus.SandboxUnavailable
            ? new DayRunResult(DayRunKind.SandboxRefused, run, run.Error)
            : new DayRunResult(DayRunKind.Ran, run);
    }

    /// <summary>The template file the run command points at: the first one with one of the plugin's extensions.</summary>
    private static string SourceName(PluginManifest plugin) =>
        plugin.TemplateFiles.Keys.FirstOrDefault(n => plugin.Extensions.Contains(Path.GetExtension(n), StringComparer.OrdinalIgnoreCase)) ?? plugin.TemplateFiles.Keys.First();

    private async Task<string> WriteInputAsync(int year, int day, string text, CancellationToken cancellationToken)
    {
        var path = Path.Combine(appData.Root, "inputs", year.ToString(CultureInfo.InvariantCulture), day.ToString("00", CultureInfo.InvariantCulture) + ".txt");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, text, cancellationToken);
        return path;
    }
}
