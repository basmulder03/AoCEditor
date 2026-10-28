using Avalonia;
using Session.UI;
using Shouldly;
using Xunit;

namespace Session.Tests;

public sealed class SignInWindowPlacementTests
{
    private static readonly PixelRect Left = new(0, 0, 1920, 1080);
    private static readonly PixelRect Right = new(1920, 0, 1920, 1040);
    private static readonly PixelSize Desired = new(520, 720);

    [Fact]
    public void CentersOnTheOwner()
    {
        var owner = new PixelRect(2200, 100, 1000, 800);

        var placed = SignInWindowPlacement.Place(owner, [Left, Right], Desired);

        placed.ShouldBe(new PixelRect(2440, 140, 520, 720));
    }

    [Fact]
    public void StaysOnTheScreenOfTheOwner()
    {
        var placed = SignInWindowPlacement.Place(new PixelRect(100, 100, 800, 600), [Left, Right], Desired);

        Left.Contains(placed).ShouldBeTrue();
    }

    [Fact]
    public void OwnerHangingOffTheScreenEdgeStillGivesAFullyVisibleWindow()
    {
        var owner = new PixelRect(3700, 900, 600, 500);

        var placed = SignInWindowPlacement.Place(owner, [Left, Right], Desired);

        Right.Contains(placed).ShouldBeTrue();
    }

    [Fact]
    public void NeverLargerThanTheWorkingArea()
    {
        var small = new PixelRect(0, 0, 400, 500);

        var placed = SignInWindowPlacement.Place(new PixelRect(0, 0, 400, 500), [small], Desired);

        placed.ShouldBe(small);
    }

    [Fact]
    public void OwnerOutsideEveryScreenFallsBackToTheFirst()
    {
        var placed = SignInWindowPlacement.Place(new PixelRect(-5000, -5000, 100, 100), [Left, Right], Desired);

        Left.Contains(placed).ShouldBeTrue();
    }
}
