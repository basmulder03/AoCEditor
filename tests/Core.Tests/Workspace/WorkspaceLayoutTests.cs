using Shouldly;
using Workspace.Domain;
using Xunit;

namespace Workspace.Tests;

public class WorkspaceLayoutTests
{
    private static readonly string Root = Path.GetFullPath("solutions", Path.GetTempPath());

    [Fact]
    public void ADaysSolutionLivesUnderYearDayAndLanguage()
    {
        WorkspaceLayout.DayFolder(Root, 2024, 3, "python").ShouldBe(Path.Combine(Root, "2024", "03", "python"));
    }

    [Fact]
    public void SharedHelpersLiveUnderSharedAndLanguage()
    {
        WorkspaceLayout.SharedFolder(Root, "dotnet").ShouldBe(Path.Combine(Root, "shared", "dotnet"));
    }

    [Theory]
    [InlineData(2014, 1, "python")]
    [InlineData(10000, 1, "python")]
    [InlineData(2024, 0, "python")]
    [InlineData(2024, 26, "python")]
    [InlineData(2024, 1, "")]
    [InlineData(2024, 1, "..")]
    [InlineData(2024, 1, "../other")]
    [InlineData(2024, 1, "a/b")]
    [InlineData(2024, 1, "Py thon")]
    public void OutOfRangeValuesAndUnsafeLanguageIdsAreRefused(int year, int day, string language)
    {
        Should.Throw<ArgumentException>(() => WorkspaceLayout.DayFolder(Root, year, day, language));
    }
}
