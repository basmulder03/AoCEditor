using AoCEditor;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaEdit;
using Shouldly;
using Xunit;

namespace Ui.Tests;

public sealed class SolutionPanelTests : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("panel").FullName;

    public void Dispose() => Directory.Delete(_folder, true);

    private async Task<(AppServices Services, SolutionPanel Panel, Window Window)> OpenAsync()
    {
        var services = AccountTests.Create(new AccountTests.Store());
        await services.Workspace.SetRootAsync(_folder, TestContext.Current.CancellationToken);
        var panel = new SolutionPanel(services, 2024, 3);
        var window = new Window { Content = panel, Width = 800, Height = 600 };
        window.Show();
        for (var i = 0; i < 50 && panel.Editor is null; i++)
        {
            await Task.Delay(50, TestContext.Current.CancellationToken);
            Dispatcher.UIThread.RunJobs();
        }

        return (services, panel, window);
    }

    [AvaloniaFact]
    public async Task OpeningWithAWorkspaceShowsTheScaffoldedSourceInANamedEditor()
    {
        var (services, panel, _) = await OpenAsync();
        using var _ = services;

        var editor = panel.GetVisualDescendants().OfType<TextEditor>().Single();
        AutomationProperties.GetName(editor).ShouldBe("Code editor");
        panel.Editor!.FilePath.ShouldStartWith(Path.Combine(_folder, "2024", "03", services.Plugins[0].Id));
        editor.Text.ShouldNotBeEmpty();
    }

    [AvaloniaFact]
    public async Task RunSavesUnsavedEditsBeforeRunning()
    {
        var (services, panel, _) = await OpenAsync();
        using var _ = services;
        panel.Editor!.Text = "edited by the user";

        panel.GetVisualDescendants().OfType<Button>().Single(b => AutomationProperties.GetName(b) == "Run solution").RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        await Task.Delay(300, TestContext.Current.CancellationToken);

        File.ReadAllText(panel.Editor.FilePath!).ShouldBe("edited by the user");
    }

    [AvaloniaFact]
    public async Task ActivatingTheWindowNoticesAChangeOnDisk()
    {
        var (services, panel, window) = await OpenAsync();
        using var _ = services;
        File.WriteAllText(panel.Editor!.FilePath!, "changed elsewhere");

        window.Activate();
        Dispatcher.UIThread.RunJobs();

        panel.Editor.ChangedOnDisk.ShouldBeTrue();
    }
}
