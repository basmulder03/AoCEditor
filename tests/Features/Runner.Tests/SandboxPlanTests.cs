using Runner.Application;
using Shouldly;
using Xunit;

namespace Runner.Tests;

public sealed class SandboxPlanTests
{
    [Fact]
    public void ThePlanSurvivesTheTripThroughTheHelperArguments()
    {
        var plan = new SandboxPlan(
            ["/usr", "/a b/c;d"],
            ["/tmp/x"],
            new SandboxLimits(1024, 5, 7, 2048),
            Filesystem: true,
            Network: true,
            ["prog", "--", "$(x)", "-w", ""]);

        var parsed = SandboxPlan.TryParse(plan.ToArguments());

        parsed.ShouldNotBeNull();
        parsed.Readable.ShouldBe(plan.Readable);
        parsed.Writable.ShouldBe(plan.Writable);
        parsed.Limits.ShouldBe(plan.Limits);
        parsed.Filesystem.ShouldBeTrue();
        parsed.Network.ShouldBeTrue();
        parsed.Command.ShouldBe(plan.Command);
    }

    [Fact]
    public void ArgumentsThatAreNotForTheHelperAreIgnored()
    {
        SandboxPlan.TryParse(["--version"]).ShouldBeNull();
        SandboxPlan.TryParse([]).ShouldBeNull();
    }

    [Fact]
    public void AMalformedPlanIsRejectedInsteadOfRunningWithFewerRestrictions()
    {
        Should.Throw<FormatException>(() => SandboxPlan.TryParse([SandboxPlan.Flag, "--mem", "lots", "--", "x"]));
        Should.Throw<FormatException>(() => SandboxPlan.TryParse([SandboxPlan.Flag, "--mem", "1"]));
    }

    [Theory]
    [InlineData(SandboxLevel.Full, "")]
    [InlineData(SandboxLevel.None, "resource limits, filesystem isolation, network isolation")]
    [InlineData(SandboxLevel.Limits, "filesystem isolation, network isolation")]
    [InlineData(SandboxLevel.Limits | SandboxLevel.Network, "filesystem isolation")]
    public void TheMissingProtectionsAreNamed(SandboxLevel level, string expected) => level.Missing().ShouldBe(expected);

    [Fact]
    public void WithoutLinuxNothingIsDetected()
    {
        if (!OperatingSystem.IsLinux())
        {
            LinuxSandbox.Detect().ShouldBe(SandboxLevel.None);
        }
    }
}
