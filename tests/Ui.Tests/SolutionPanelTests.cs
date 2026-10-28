using AoCEditor;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaEdit;
using Shouldly;
using Xunit;

namespace AoCEditor.UI.Tests;

public sealed class SolutionPanelTests : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("panel").FullName;

    public void Dispose() => Directory.Delete(_folder, true);

    private async Task<(TestApp App, SolutionPanel Panel, Window Window)> OpenAsync()
    {
        var app = AccountTests.Create(new AccountTests.Store());
        var services = app.Services;
        await services.Workspace.SetRootAsync(_folder, TestContext.Current.CancellationToken);
        var panel = new SolutionPanel(services, 2024, 3);
        var window = new Window { Content = panel, Width = 800, Height = 600 };
        window.Show();
        for (var i = 0; i < 50 && panel.Editor is null; i++)
        {
            await Task.Delay(50, TestContext.Current.CancellationToken);
            Dispatcher.UIThread.RunJobs();
        }

        return (app, panel, window);
    }

    [AvaloniaFact]
    public async Task OpeningWithAWorkspaceShowsTheScaffoldedSourceInANamedEditor()
    {
        var (app, panel, _) = await OpenAsync();
        var services = app.Services;
        using var _ = app;

        var editor = panel.GetVisualDescendants().OfType<TextEditor>().Single();
        AutomationProperties.GetName(editor).ShouldBe("Code editor");
        panel.Editor!.FilePath.ShouldStartWith(Path.Combine(_folder, "2024", "03", services.Plugins[0].Id));
        editor.Text.ShouldNotBeEmpty();
    }

    [AvaloniaFact]
    public async Task RunSavesUnsavedEditsBeforeRunning()
    {
        var (app, panel, _) = await OpenAsync();
        var services = app.Services;
        using var _ = app;
        panel.Editor!.Text = "edited by the user";

        panel.GetVisualDescendants().OfType<Button>().Single(b => AutomationProperties.GetName(b) == "Run solution").RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        await Task.Delay(300, TestContext.Current.CancellationToken);

        File.ReadAllText(panel.Editor.FilePath!).ShouldBe("edited by the user");
    }

    [AvaloniaFact]
    public async Task ActivatingTheWindowNoticesAChangeOnDisk()
    {
        var (app, panel, window) = await OpenAsync();
        var services = app.Services;
        using var _ = app;
        File.WriteAllText(panel.Editor!.FilePath!, "changed elsewhere");

        window.Activate();
        Dispatcher.UIThread.RunJobs();

        panel.Editor.ChangedOnDisk.ShouldBeTrue();
    }

    private static ComboBox Language(SolutionPanel panel) => panel.GetVisualDescendants().OfType<ComboBox>().Single();

    private static async Task SettleAsync()
    {
        await Task.Delay(300, TestContext.Current.CancellationToken);
        Dispatcher.UIThread.RunJobs();
    }

    [AvaloniaFact]
    public async Task SwitchingLanguageWithAConflictingUnsavedEditKeepsTheFileAndLanguage()
    {
        var (app, panel, _) = await OpenAsync();
        var services = app.Services;
        using var _ = app;
        var editor = panel.Editor!;
        var path = editor.FilePath!;
        editor.Text = "my unsaved work";
        File.WriteAllText(path, "changed elsewhere");

        Language(panel).SelectedIndex = 1;
        await SettleAsync();

        Language(panel).SelectedIndex.ShouldBe(0);
        panel.Editor.ShouldBeSameAs(editor);
        editor.Text.ShouldBe("my unsaved work");
        editor.ChangedOnDisk.ShouldBeTrue();
        File.ReadAllText(path).ShouldBe("changed elsewhere");
    }

    [AvaloniaFact]
    public async Task ChoosingAFolderWithAConflictingUnsavedEditKeepsTheFile()
    {
        var (app, panel, _) = await OpenAsync();
        var services = app.Services;
        using var _ = app;
        var editor = panel.Editor!;
        editor.Text = "my unsaved work";
        File.WriteAllText(editor.FilePath!, "changed elsewhere");
        var other = Directory.CreateTempSubdirectory("panel-other").FullName;

        try
        {
            await panel.ApplyFolderAsync(other);
            await SettleAsync();

            panel.Editor.ShouldBeSameAs(editor);
            editor.Text.ShouldBe("my unsaved work");
            editor.ChangedOnDisk.ShouldBeTrue();
            (await services.Workspace.RootAsync(TestContext.Current.CancellationToken)).ShouldBe(_folder);
        }
        finally
        {
            Directory.Delete(other, true);
        }
    }
}
