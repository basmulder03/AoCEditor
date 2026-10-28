using System.Diagnostics;
using Shouldly;
using Xunit;

namespace Shared.Tests;

public sealed class PracticeRuleTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "aoc-rules-" + Guid.NewGuid().ToString("N"));

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private (int Exit, string Output) Run(string code)
    {
        Directory.CreateDirectory(_root);
        File.WriteAllText(Path.Combine(_root, "Sample.cs"), code);
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

    [Theory]
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

    [Theory]
    [InlineData("_log.Attempt(page, 1);")]
    [InlineData("_log.SignedOut();")]
    [InlineData("var factory = logs.CreateLogger(\"Session.SignIn\");")]
    public void OrdinaryLoggingPasses(string line)
    {
        var (exit, _) = Run(line);

        exit.ShouldBe(0);
    }
}
