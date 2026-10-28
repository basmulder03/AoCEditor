using AoCEditor.Core.Plugins.Domain;
using Shouldly;
using Xunit;

namespace AoCEditor.Core.Tests.Plugins;

public class ManifestParserTests
{
    private const string Valid = """
        {
          "schemaVersion": 1,
          "id": "python",
          "name": "Python",
          "extensions": [".py"],
          "templateFiles": { "main.py": "print(1)\n" },
          "toolchain": { "executables": ["python3", "python"] },
          "run": { "command": ["{toolchain}", "{source}"], "input": "stdin" }
        }
        """;

    [Fact]
    public void AValidManifestIsReadIntoTheModel()
    {
        var result = ManifestParser.Parse(Valid);

        result.Errors.ShouldBeEmpty();
        var m = result.Manifest!;
        m.Id.ShouldBe("python");
        m.Name.ShouldBe("Python");
        m.Extensions.ShouldBe([".py"]);
        m.TemplateFiles["main.py"].ShouldBe("print(1)\n");
        m.ToolchainExecutables.ShouldBe(["python3", "python"]);
        m.Compile.ShouldBeNull();
        m.Run.Command.ShouldBe(["{toolchain}", "{source}"]);
        m.Run.Input.ShouldBe(InputMode.Stdin);
    }

    [Fact]
    public void ACompileStepIsOptional()
    {
        var json = Valid.Replace("\"run\"", "\"compile\": { \"command\": [\"{toolchain}\", \"build\"] }, \"run\"");

        ManifestParser.Parse(json).Manifest!.Compile!.Command.ShouldBe(["{toolchain}", "build"]);
    }

    [Theory]
    [InlineData("""{ "schemaVersion": 2 }""", "schema version")]
    [InlineData("""{ "schemaVersion": 1, "id": "Py Thon", "name": "P", "extensions": [".py"], "toolchain": { "executables": ["p"] }, "run": { "command": ["{source}"], "input": "stdin" } }""", "id")]
    [InlineData("""{ "schemaVersion": 1, "id": "p", "name": "P", "extensions": ["py"], "toolchain": { "executables": ["p"] }, "run": { "command": ["{source}"], "input": "stdin" } }""", "extension")]
    [InlineData("""{ "schemaVersion": 1, "id": "p", "name": "P", "extensions": [".py"], "toolchain": { "executables": ["p"] }, "run": { "command": [], "input": "stdin" } }""", "run command")]
    [InlineData("""{ "schemaVersion": 1, "id": "p", "name": "P", "extensions": [".py"], "toolchain": { "executables": ["p"] }, "run": { "command": ["{shell}"], "input": "stdin" } }""", "placeholder")]
    [InlineData("""{ "schemaVersion": 1, "id": "p", "name": "P", "extensions": [".py"], "toolchain": { "executables": ["p"] }, "run": { "command": ["{source}"], "input": "pipe" } }""", "input")]
    [InlineData("""{ "schemaVersion": 1, "id": "p", "name": "P", "extensions": [".py"], "templateFiles": { "../x": "" }, "toolchain": { "executables": ["p"] }, "run": { "command": ["{source}"], "input": "stdin" } }""", "template file")]
    [InlineData("""{ "schemaVersion": 1, "id": "p", "name": "P", "extensions": [".py"], "toolchain": { "executables": ["a/b"] }, "run": { "command": ["{source}"], "input": "stdin" } }""", "executable")]
    [InlineData("""{ "schemaVersion": 1, "id": "p", "name": "P", "extensions": [".py"], "toolchain": { "executables": ["p"] }, "run": { "command": ["{source}"], "input": "stdin" } }""", "template file")]
    [InlineData("not json", "JSON")]
    [InlineData("", "JSON")]
    public void InvalidManifestsAreRejectedWithAReadableReason(string json, string reason)
    {
        var result = ManifestParser.Parse(json);

        result.Manifest.ShouldBeNull();
        result.Errors.ShouldContain(e => e.Contains(reason, StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("C:evil.cs")]
    [InlineData("name:stream")]
    [InlineData("CON")]
    [InlineData("nul.txt")]
    public void UnsafeTemplateAndExecutableNamesAreRejected(string name)
    {
        var template = ManifestParser.Parse(Valid.Replace("\"main.py\":", $"\"{name}\":"));
        var executable = ManifestParser.Parse(Valid.Replace("\"python3\"", $"\"{name}\""));

        template.Manifest.ShouldBeNull();
        template.Errors.ShouldNotBeEmpty();
        executable.Manifest.ShouldBeNull();
        executable.Errors.ShouldNotBeEmpty();
    }
}
