using System.Text;
using AoCEditor.UI.Editor;
using Shouldly;
using Xunit;

namespace AoCEditor.UI.Tests.Editor;

public sealed class EditorViewModelTests : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("editor").FullName;

    public void Dispose() => Directory.Delete(_folder, true);

    private string Write(string name, string content, bool bom = false)
    {
        var path = Path.Combine(_folder, name);
        File.WriteAllBytes(path, [.. (bom ? Encoding.UTF8.GetPreamble() : []), .. Encoding.UTF8.GetBytes(content)]);
        return path;
    }

    [Fact]
    public void LoadShowsTheTextAndIsClean()
    {
        var path = Write("a.py", "print(1)\n");
        var model = new EditorViewModel(_folder);

        model.Load(path);

        model.Text.ShouldBe("print(1)\n");
        model.IsDirty.ShouldBeFalse();
        model.FilePath.ShouldBe(path);
    }

    [Fact]
    public void EditingMakesItDirtyAndSavingWritesUtf8WithoutBom()
    {
        var path = Write("a.py", "x");
        var model = new EditorViewModel(_folder);
        model.Load(path);

        model.Text = "héllo";
        model.IsDirty.ShouldBeTrue();
        model.Save().ShouldBe(SaveResult.Saved);

        model.IsDirty.ShouldBeFalse();
        File.ReadAllBytes(path).ShouldBe(Encoding.UTF8.GetBytes("héllo"));
    }

    [Fact]
    public void ABomAndCrLfLineEndingsAreKept()
    {
        var path = Write("a.cs", "a\r\nb\r\n", bom: true);
        var model = new EditorViewModel(_folder);
        model.Load(path);

        model.Text = "a\nb\nc\n";
        model.Save();

        File.ReadAllBytes(path).ShouldBe([.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes("a\r\nb\r\nc\r\n")]);
    }

    [Fact]
    public void AnExternalChangeIsFlaggedAndSaveRefusesToOverwriteIt()
    {
        var path = Write("a.py", "one");
        var model = new EditorViewModel(_folder);
        model.Load(path);
        model.Text = "mine";
        File.WriteAllText(path, "theirs");

        model.CheckDisk();

        model.ChangedOnDisk.ShouldBeTrue();
        model.Save().ShouldBe(SaveResult.ChangedOnDisk);
        File.ReadAllText(path).ShouldBe("theirs");
        model.Save(overwrite: true).ShouldBe(SaveResult.Saved);
        File.ReadAllText(path).ShouldBe("mine");
        model.ChangedOnDisk.ShouldBeFalse();
    }

    [Fact]
    public void ASaveDetectsAChangeEvenWithoutAPriorCheck()
    {
        var path = Write("a.py", "one");
        var model = new EditorViewModel(_folder);
        model.Load(path);
        File.WriteAllText(path, "theirs");

        model.Save().ShouldBe(SaveResult.ChangedOnDisk);
    }

    [Fact]
    public void ReloadDiscardsEditsAndClearsTheChangedState()
    {
        var path = Write("a.py", "one");
        var model = new EditorViewModel(_folder);
        model.Load(path);
        model.Text = "mine";
        File.WriteAllText(path, "theirs");
        model.CheckDisk();

        model.Reload();

        model.Text.ShouldBe("theirs");
        model.IsDirty.ShouldBeFalse();
        model.ChangedOnDisk.ShouldBeFalse();
    }

    [Fact]
    public void ARewriteWithIdenticalContentIsNotAChange()
    {
        var path = Write("a.py", "one");
        var model = new EditorViewModel(_folder);
        model.Load(path);
        File.WriteAllText(path, "one");

        model.CheckDisk();

        model.ChangedOnDisk.ShouldBeFalse();
    }

    [Fact]
    public void ADeletedFileIsReportedAsChanged()
    {
        var path = Write("a.py", "one");
        var model = new EditorViewModel(_folder);
        model.Load(path);
        File.Delete(path);

        model.CheckDisk();

        model.ChangedOnDisk.ShouldBeTrue();
    }

    [Theory]
    [InlineData("../outside.py")]
    [InlineData("sub/../../outside.py")]
    public void PathsOutsideTheFolderAreRejected(string relative)
    {
        var model = new EditorViewModel(_folder);

        Should.Throw<ArgumentException>(() => model.Load(Path.Combine(_folder, relative)));
        Should.Throw<ArgumentException>(() => model.Load(Path.Combine(Path.GetTempPath(), "x.py")));
    }

    [Fact]
    public void ANewFileInsideTheFolderCanBeCreatedBySaving()
    {
        var path = Path.Combine(_folder, "new.py");
        var model = new EditorViewModel(_folder);
        model.Load(path);

        model.Text = "x";
        model.Save().ShouldBe(SaveResult.Saved);

        File.ReadAllText(path).ShouldBe("x");
    }

    [Theory]
    [InlineData("a.py", "source.python")]
    [InlineData("a.cs", "source.cs")]
    [InlineData("a.unknown", null)]
    public void HighlightingFollowsTheExtension(string file, string? expected)
    {
        Highlighting.ScopeFor(file).ShouldBe(expected);
    }
}
