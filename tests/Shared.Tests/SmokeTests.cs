using Shouldly;
using Xunit;

namespace Shared.Tests;

public class SmokeTests
{
    [Fact]
    public void TestRunnerWorks() => (1 + 1).ShouldBe(2);
}
