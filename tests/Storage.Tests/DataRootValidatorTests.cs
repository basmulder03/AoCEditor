using Shouldly;
using Storage.Domain;
using Xunit;

namespace Storage.Tests;

public class DataRootValidatorTests
{
    [Theory]
    [InlineData("/home/u/solutions", "/home/u/data", true)]
    [InlineData("/home/u/solutions", "/home/u/solutions-data", true)]
    [InlineData("/home/u/solutions", "/home/u/solutions/data", false)]
    [InlineData("/home/u/solutions", "/home/u/solutions", false)]
    [InlineData("/home/u/solutions/", "/home/u/solutions/../solutions/x", false)]
    [InlineData("/home/u/solutions/aoc", "/home/u", false)]
    public void DataRootMustNotOverlapTheSolutionsFolder(string solutions, string dataRoot, bool allowed)
    {
        DataRootValidator.IsAllowed(dataRoot, solutions).ShouldBe(allowed);
    }
}
