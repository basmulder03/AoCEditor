using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using AvaloniaEdit;
using Editor.Application;
using Editor.UI;
using Shouldly;

namespace Ui.Tests;

public sealed class EditorViewTests : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("editorview").FullName;

    public void Dispose() => Directory.Delete(_folder, true);

    private (EditorViewModel Model, EditorView View, TextEditor Editor, Window Window) Open(string name, string content)
    {
        var path = Path.Combine(_folder, name);
        File.WriteAllText(path, content);
        var model = new EditorViewModel(_folder);
        model.Load(path);
        var view = new EditorView { DataContext = model };
        var window = new Window { Content = view, Width = 600, Height = 300 };
        window.Show();
        return (model, view, view.GetVisualDescendants().OfType<TextEditor>().Single(), window);
    }

    [AvaloniaFact]
    public void TheEditorIsNamedShowsTheTextAndUsesAMonospaceFont()
    {
        var (_, _, editor, _) = Open("a.py", "print(1)");

        AutomationProperties.GetName(editor).ShouldBe("Code editor");
        editor.Text.ShouldBe("print(1)");
        editor.FontFamily.ToString().ShouldContain("monospace");
    }

    [AvaloniaFact]
    public void TypingMarksItDirtyAndCtrlSSavesIt()
    {
        var (model, view, editor, _) = Open("a.py", "x");
        editor.Text = "y";
        model.IsDirty.ShouldBeTrue();

        view.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.S, KeyModifiers = KeyModifiers.Control });

        model.IsDirty.ShouldBeFalse();
        File.ReadAllText(Path.Combine(_folder, "a.py")).ShouldBe("y");
    }

    [AvaloniaFact]
    public void ReloadingFromDiskUpdatesTheEditor()
    {
        var (model, _, editor, _) = Open("a.py", "x");
        File.WriteAllText(Path.Combine(_folder, "a.py"), "z");

        model.Reload();

        editor.Text.ShouldBe("z");
    }
}
