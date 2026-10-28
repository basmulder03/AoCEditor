using AoCEditor;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Session.Application;
using Shouldly;
using Storage.Infrastructure;
using Xunit;

namespace Ui.Tests;

public class AccountTests
{
    [AvaloniaFact]
    public void SignInRequiredShowsANonBlockingPromptWithASignInButton()
    {
        using var services = Create(new Store());
        var shell = new ShellWindow(services);
        shell.Show();
        shell.IsSignInPromptVisible.ShouldBeFalse();

        services.Events.NotifySignInRequired();
        Dispatcher.UIThread.RunJobs();

        shell.IsSignInPromptVisible.ShouldBeTrue();
        var button = shell.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PromptSignIn");
        button.IsTabStop.ShouldBeTrue();
        Avalonia.Automation.AutomationProperties.GetName(button).ShouldBe("Sign in");
    }

    [AvaloniaFact]
    public void AccountSectionControlsAreNamedForAssistiveTechnology()
    {
        using var services = Create(new Store());
        var shell = new ShellWindow(services);
        shell.Show();
        shell.NavigateTo("Settings");

        var names = shell.GetLogicalDescendants().OfType<Control>()
            .Where(c => c.Name is "SignInButton" or "SignOutButton" or "PasteToggle" or "PasteBox" or "UseTokenButton")
            .Select(c => Avalonia.Automation.AutomationProperties.GetName(c))
            .ToList();

        names.Count.ShouldBe(5);
        names.ShouldAllBe(n => !string.IsNullOrWhiteSpace(n));
    }

    [AvaloniaFact]
    public async Task UnavailableSecureStorageIsExplainedAndSigningInIsDisabled()
    {
        using var services = Create(new Store { Down = true });
        var shell = new ShellWindow(services);
        shell.Show();
        shell.NavigateTo("Settings");
        await Task.Delay(200, TestContext.Current.CancellationToken);
        Dispatcher.UIThread.RunJobs();

        var controls = shell.GetVisualDescendants().OfType<Control>().ToList();
        controls.OfType<TextBlock>().Single(t => t.Name == "StatusText").Text.ShouldNotBeNull().ShouldContain("Secure storage is unavailable");
        controls.OfType<Button>().Single(b => b.Name == "SignInButton").IsEnabled.ShouldBeFalse();
    }

    private static AppServices Create(Store store) =>
        new(store, new AppDataPaths(Path.Combine(Path.GetTempPath(), "aoceditor-ui-" + Guid.NewGuid().ToString("N"))));

    private sealed class Store : ISecretStore
    {
        public bool Down { get; init; }

        public Task<SecretResult> GetAsync(string name, CancellationToken cancellationToken) =>
            Task.FromResult<SecretResult>(Down ? new SecretResult.Unavailable("no service") : new SecretResult.NotFound());

        public Task<SecretResult> SetAsync(string name, string value, CancellationToken cancellationToken) =>
            Task.FromResult<SecretResult>(new SecretResult.Ok());

        public Task<SecretResult> DeleteAsync(string name, CancellationToken cancellationToken) =>
            Task.FromResult<SecretResult>(new SecretResult.Ok());
    }
}
