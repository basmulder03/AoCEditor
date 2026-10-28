using AoCEditor.Core.Runner.Application;
using AoCEditor.UI.Runner;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Shouldly;

namespace AoCEditor.UI.Tests;

public class ConsoleViewTests
{
    [AvaloniaFact]
    public void OutputLinesAndTheStatusAreShown()
    {
        var model = new ConsoleViewModel();
        var view = new ConsoleView { DataContext = model };
        var window = new Window { Content = view, Width = 600, Height = 300 };
        window.Show();

        model.Begin();
        model.Append(new OutputLine(OutputChannel.Stdout, "hello"));
        model.Append(new OutputLine(OutputChannel.Stderr, "oops"));
        model.End(new RunResult(RunStatus.Completed, 0, TimeSpan.FromSeconds(1)));

        window.UpdateLayout();
        var texts = view.GetVisualDescendants().OfType<SelectableTextBlock>().Select(t => t.Text).ToList();
        texts.ShouldBe(["hello", "oops"]);
        view.GetVisualDescendants().OfType<SelectableTextBlock>().Last().Classes.ShouldContain("error");
        view.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text).ShouldContain("Finished with exit code 0 in 1.00 s");
    }
}
