using Plugins.Domain;
using Shouldly;
using Xunit;

namespace Plugins.Tests;

public class CommandTemplateTests
{
    [Fact]
    public void PlaceholdersAreFilledPerArgumentAndNeverSplit()
    {
        var values = new Dictionary<string, string> { ["toolchain"] = "/usr/bin/python3", ["source"] = "/home/me/my solutions/main.py" };

        CommandTemplate.Expand(["{toolchain}", "-u", "{source}"], values).ShouldBe(["/usr/bin/python3", "-u", "/home/me/my solutions/main.py"]);
    }

    [Fact]
    public void AValueIsNotReinterpretedAsAPlaceholderOrShellSyntax()
    {
        var values = new Dictionary<string, string> { ["source"] = "a;{input}$(rm -rf ~)", ["input"] = "X" };

        CommandTemplate.Expand(["--file={source}"], values).ShouldBe(["--file=a;{input}$(rm -rf ~)"]);
    }

    [Fact]
    public void AMissingValueIsAnError()
    {
        Should.Throw<ArgumentException>(() => CommandTemplate.Expand(["{input}"], new Dictionary<string, string>()));
    }
}
