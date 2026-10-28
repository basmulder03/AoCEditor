using AoCEditor.Core.Diagnostics.Application;
using Shouldly;
using Xunit;

namespace AoCEditor.Core.Tests.Diagnostics;

public sealed class DebugModeTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public void OffByDefault()
    {
        DebugMode.FromLaunch([], _ => null).IsEnabled.ShouldBeFalse();
    }

    [Fact]
    public void FlagTurnsItOn()
    {
        DebugMode.FromLaunch(["--debug"], _ => null).IsEnabled.ShouldBeTrue();
    }

    [Theory]
    [InlineData("1", true)]
    [InlineData("true", true)]
    [InlineData("TRUE", true)]
    [InlineData("0", false)]
    [InlineData("", false)]
    [InlineData("no", false)]
    public void EnvironmentVariableIsRead(string value, bool expected)
    {
        var mode = DebugMode.FromLaunch([], name => name == DebugMode.EnvironmentVariable ? value : null);

        mode.IsEnabled.ShouldBe(expected);
    }

    [Fact]
    public async Task PersistedSettingTurnsItOn()
    {
        var store = new MemoryStore();
        store.Items[DebugMode.SettingKey] = "true";
        var mode = DebugMode.FromLaunch([], _ => null);

        await mode.LoadAsync(store, Ct);

        mode.IsEnabled.ShouldBeTrue();
    }

    [Fact]
    public async Task SettingIsPersistedAndRaisesChanged()
    {
        var store = new MemoryStore();
        var mode = DebugMode.FromLaunch([], _ => null);
        await mode.LoadAsync(store, Ct);
        var raised = 0;
        mode.Changed += (_, _) => raised++;

        await mode.SetAsync(true, Ct);

        mode.IsEnabled.ShouldBeTrue();
        raised.ShouldBe(1);
        store.Items[DebugMode.SettingKey].ShouldBe("true");

        await mode.SetAsync(false, Ct);
        mode.IsEnabled.ShouldBeFalse();
        store.Items[DebugMode.SettingKey].ShouldBe("false");
    }

    [Fact]
    public async Task LaunchOverrideSurvivesTheSettingBeingOff()
    {
        var store = new MemoryStore();
        var mode = DebugMode.FromLaunch(["--debug"], _ => null);
        await mode.LoadAsync(store, Ct);

        await mode.SetAsync(false, Ct);

        mode.IsEnabled.ShouldBeTrue();
        mode.IsForced.ShouldBeTrue();
    }
}
