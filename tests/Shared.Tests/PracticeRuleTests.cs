using System.Diagnostics;
using Shouldly;
using Xunit;

namespace Shared.Tests;

public sealed class PracticeRuleTests : IDisposable
{
    // The script runs in the Linux CI job; Windows runners resolve a different bash.
    public static bool RunsHere => !OperatingSystem.IsWindows();

    private readonly string _root = Path.Combine(Path.GetTempPath(), "aoc-rules-" + Guid.NewGuid().ToString("N"));

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private (int Exit, string Output) Run(string code, string fileName = "Sample.cs")
    {
        Directory.CreateDirectory(_root);
        File.WriteAllText(Path.Combine(_root, fileName), code);
        var dir = AppContext.BaseDirectory;
        while (!File.Exists(Path.Combine(dir, "scripts", "check-practices.sh")))
        {
            dir = Path.GetDirectoryName(dir) ?? throw new InvalidOperationException("repository root not found");
        }

        var info = new ProcessStartInfo("bash", [Path.Combine(dir, "scripts", "check-practices.sh")]) { RedirectStandardOutput = true };
        info.Environment["CHECK_ROOT"] = _root;
        using var process = Process.Start(info)!;
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return (process.ExitCode, output);
    }

    [Theory(Skip = "Practice script runs on Linux only", SkipUnless = nameof(RunsHere), SkipType = typeof(PracticeRuleTests))]
    [InlineData("_log.Attempt(token);")]
    [InlineData("logger.Attempt(request.Cookie);")]
    [InlineData("_log.Attempt(sessionValue, 1);")]
    [InlineData("public static partial void Attempt(this ILogger logger, string token);")]
    public void LoggingASecretNamedValueIsFlagged(string line)
    {
        var (exit, output) = Run(line);

        exit.ShouldBe(1);
        output.ShouldContain("Logging calls or log parameters named like secrets");
    }

    [Theory(Skip = "Practice script runs on Linux only", SkipUnless = nameof(RunsHere), SkipType = typeof(PracticeRuleTests))]
    [InlineData("_log.Attempt(page, 1);")]
    [InlineData("_log.SignedOut();")]
    [InlineData("var factory = logs.CreateLogger(\"Session.SignIn\");")]
    public void OrdinaryLoggingPasses(string line)
    {
        var (exit, _) = Run(line);

        exit.ShouldBe(0);
    }

    [Theory(Skip = "Practice script runs on Linux only", SkipUnless = nameof(RunsHere), SkipType = typeof(PracticeRuleTests))]
    [InlineData("namespace N;\npublic sealed class Sample;\npublic enum Other { A }\n", "Sample.cs")]
    [InlineData("namespace N;\npublic delegate void Sample();\npublic interface ISample;\n", "Sample.cs")]
    [InlineData("namespace N;\npublic sealed class Different;\n", "Sample.cs")]
    [InlineData("namespace N\n{\n    public sealed class Sample;\n    public record Other(int A);\n}\n", "Sample.cs")]
    public void SeveralTopLevelTypesOrAMismatchedFileNameIsFlagged(string code, string fileName)
    {
        var (exit, output) = Run(code, fileName);

        exit.ShouldBe(1);
        output.ShouldContain("One top-level type per file");
    }

    [Theory(Skip = "Practice script runs on Linux only", SkipUnless = nameof(RunsHere), SkipType = typeof(PracticeRuleTests))]
    [InlineData("namespace N;\npublic sealed class Sample\n{\n    private sealed class Nested;\n    public enum Kind { A }\n}\n")]
    [InlineData("namespace N;\npublic sealed partial class Sample<T>\n{\n}\n")]
    [InlineData("namespace N;\npublic delegate void Sample(int a);\n")]
    public void OneTopLevelTypeNamedAfterTheFilePasses(string code)
    {
        var (exit, _) = Run(code);

        exit.ShouldBe(0);
    }
}
