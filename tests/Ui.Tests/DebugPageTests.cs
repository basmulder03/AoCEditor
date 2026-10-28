using AoCEditor;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Diagnostics.Application;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;

namespace Ui.Tests;

public class DebugPageTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static IEnumerable<Control> Controls(Control root) => root.GetLogicalDescendants().OfType<Control>();

    [AvaloniaFact]
    public void DebugEntryIsHiddenOutsideDebugMode()
    {
        using var app = AccountTests.Create(new AccountTests.Store());
        var services = app.Services;
        var shell = new ShellWindow(services);
        shell.Show();

        shell.HasNavigationEntry("Debug").ShouldBeFalse();
    }

    [AvaloniaFact]
    public void DebugEntryAppearsWhenDebugModeIsOn()
    {
        using var app = AccountTests.Create(new AccountTests.Store(), DebugMode.FromLaunch(["--debug"], _ => null));
        var services = app.Services;
        var shell = new ShellWindow(services);
        shell.Show();

        shell.HasNavigationEntry("Debug").ShouldBeTrue();
    }

    [AvaloniaFact]
    public async Task EntryFollowsTheSettingAtRuntimeAndLeavesTheDebugPageWhenItGoesAway()
    {
        using var app = AccountTests.Create(new AccountTests.Store());
        var services = app.Services;
        var shell = new ShellWindow(services);
        shell.Show();

        await services.Diagnostics.Debug.SetAsync(true, Ct);
        Dispatcher.UIThread.RunJobs();
        shell.HasNavigationEntry("Debug").ShouldBeTrue();
        shell.NavigateTo("Debug");
        shell.CurrentPage.ShouldBeOfType<DebugPage>();

        await services.Diagnostics.Debug.SetAsync(false, Ct);
        Dispatcher.UIThread.RunJobs();

        shell.HasNavigationEntry("Debug").ShouldBeFalse();
        shell.CurrentPage.ShouldNotBeOfType<DebugPage>();
    }

    [AvaloniaFact]
    public void LogListShowsRedactedEntriesAndFiltersByLevel()
    {
        using var app = AccountTests.Create(new AccountTests.Store(), DebugMode.FromLaunch(["--debug"], _ => null));
        var services = app.Services;
        var shell = new ShellWindow(services);
        shell.Show();
        var log = services.Diagnostics.LoggerFactory.CreateLogger("Gateway.AocGateway");
        log.Log(LogLevel.Information, default, "hello session=abc123", null, static (s, _) => s);
        log.Log(LogLevel.Error, default, "broken", null, static (s, _) => s);
        shell.NavigateTo("Debug");
        var page = shell.CurrentPage.ShouldBeOfType<DebugPage>();

        page.LogLines.ShouldContain(l => l.Contains("hello session=[redacted]", StringComparison.Ordinal));
        page.LogLines.ShouldNotContain(l => l.Contains("abc123", StringComparison.Ordinal));

        page.SelectLevel("Error");
        page.LogLines.ShouldAllBe(l => l.Contains(" Error ", StringComparison.Ordinal));
        page.LogLines.ShouldNotBeEmpty();

        page.SelectLevel("All");
        page.SelectSource("Nothing");
        page.LogLines.ShouldBeEmpty();
    }

    [AvaloniaFact]
    public void InteractiveControlsAreNamedAndKeyboardReachable()
    {
        using var app = AccountTests.Create(new AccountTests.Store(), DebugMode.FromLaunch(["--debug"], _ => null));
        var services = app.Services;
        var shell = new ShellWindow(services);
        shell.Show();
        shell.NavigateTo("Debug");

        var interactive = Controls(shell).Where(c => c is Button or ComboBox or ListBox && c.Name is not null && c.Name.StartsWith("Debug", StringComparison.Ordinal)).ToList();

        interactive.Count.ShouldBeGreaterThanOrEqualTo(6);
        interactive.ShouldAllBe(c => !string.IsNullOrWhiteSpace(Avalonia.Automation.AutomationProperties.GetName(c)));
        interactive.ShouldAllBe(c => c.Focusable);
    }

    [AvaloniaFact]
    public async Task CopyDiagnosticsShowsARedactedPreviewFirst()
    {
        using var app = AccountTests.Create(new AccountTests.Store(), DebugMode.FromLaunch(["--debug"], _ => null));
        var services = app.Services;
        var shell = new ShellWindow(services);
        shell.Show();
        services.Diagnostics.LoggerFactory.CreateLogger("Session.SignIn").Log(LogLevel.Warning, default, "cookie: session=abc123", null, static (s, _) => s);
        shell.NavigateTo("Debug");
        var page = shell.CurrentPage.ShouldBeOfType<DebugPage>();

        await page.PreviewDiagnosticsAsync(Ct);

        page.Preview.ShouldContain("App: ");
        page.Preview.ShouldContain("Runtime: ");
        page.Preview.ShouldContain("Session: Signed out");
        page.Preview.ShouldNotContain("abc123");
        page.IsPreviewVisible.ShouldBeTrue();
    }
}
