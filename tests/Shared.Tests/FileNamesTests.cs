using Shouldly;
using Xunit;

namespace Shared.Tests;

public class FileNamesTests
{
    [Theory]
    [InlineData("main.py", true)]
    [InlineData("Solution.csproj", true)]
    [InlineData("python3", true)]
    [InlineData("console.cs", true)]
    [InlineData("", false)]
    [InlineData(".", false)]
    [InlineData("..", false)]
    [InlineData("../main.py", false)]
    [InlineData("sub/main.py", false)]
    [InlineData("sub\\main.py", false)]
    [InlineData("C:evil.cs", false)]
    [InlineData("name:stream", false)]
    [InlineData("a\0b", false)]
    [InlineData("CON", false)]
    [InlineData("con.txt", false)]
    [InlineData("Nul", false)]
    [InlineData("PRN.cs", false)]
    [InlineData("aux", false)]
    [InlineData("COM1", false)]
    [InlineData("com9.py", false)]
    [InlineData("LPT1", false)]
    [InlineData("lpt9.txt", false)]
    [InlineData("name.", false)]
    [InlineData("name ", false)]
    public void OnlyPlainFileNamesPass(string name, bool valid) => FileNames.IsPlain(name).ShouldBe(valid);
}
