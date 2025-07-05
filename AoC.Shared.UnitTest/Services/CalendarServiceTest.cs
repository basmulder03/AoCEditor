using AoC.Shared.Services;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace AoC.Shared.UnitTest.Services;

[TestClass]
public class CalendarServiceTest
{
    [TestMethod]
    public void GetLatestPuzzleYear_BeforeFirstPuzzleDate_ReturnsPreviousYear()
    {
        // Arrange
        var firstPuzzleDate = Constants.FirstPuzzleDate;
        var mockProvider = new FakeTimeProvider(firstPuzzleDate.AddMinutes(-1).ToUniversalTime());
        var service = new CalendarService(mockProvider);

        // Act
        var result = service.GetLatestPuzzleYear();

        // Assert
        result.ShouldBe(firstPuzzleDate.Year - 1);
    }

    [TestMethod]
    public void GetLatestPuzzleYear_OnOrAfterFirstPuzzleDate_ReturnsCurrentYear()
    {
        // Arrange
        var firstPuzzleDate = Constants.FirstPuzzleDate;
        var mockProvider = new FakeTimeProvider(firstPuzzleDate.ToUniversalTime());
        var service = new CalendarService(mockProvider);

        // Act
        var result = service.GetLatestPuzzleYear();

        // Assert
        result.ShouldBe(firstPuzzleDate.Year);
    }

    [TestMethod]
    public void GetLatestPuzzleYear_MidYear_ReturnsPreviousYear()
    {
        // Arrange
        var midYear = new DateTimeOffset(Constants.FirstPuzzleDate.Year, 7, 1, 0, 0, 0, TimeSpan.Zero);
        var mockProvider = new FakeTimeProvider(midYear);
        var service = new CalendarService(mockProvider);

        // Act
        var result = service.GetLatestPuzzleYear();

        // Assert
        result.ShouldBe(midYear.Year - 1);
    }
}