using System.Diagnostics;
using Xunit;

namespace E2E;

[Trait("Category", "E2E")]
public class AppTests
{
    [Fact]
    public async Task AppPrintsVersionAndExitsZero()
    {
        var dll = Path.Combine(AppContext.BaseDirectory, "AoCEditor.dll");
        var psi = new ProcessStartInfo("dotnet", [dll]) { RedirectStandardOutput = true };
        using var process = Process.Start(psi)!;
        var output = await process.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
        await process.WaitForExitAsync(TestContext.Current.CancellationToken);

        Assert.Equal(0, process.ExitCode);
        Assert.StartsWith("AoCEditor ", output);
    }
}
