using LogVue.Services.Query;

namespace LogVue.Tests.Query;

public class LogQueryTextTests
{
    [Theory]
    [InlineData("O'Brien")]
    [InlineData(@"C:\logs\app.txt")]
    [InlineData("")]
    public void Condition_RoundTripsThroughTheParser(string value)
    {
        var node = Assert.IsType<ComparisonNode>(LogQueryParser.Parse(LogQueryText.Condition("Name", value)));

        Assert.Equal("Name", node.Field);
        Assert.Equal(value, node.Value);
    }

    [Fact]
    public void Condition_Exclude_UsesNotEqual() =>
        Assert.Equal("Name != 'x'", LogQueryText.Condition("Name", "x", exclude: true));

    [Theory]
    [InlineData(null, "B = 'x'")]
    [InlineData("timeout", "B = 'x'")]
    [InlineData("A = 1", "A = 1 and B = 'x'")]
    [InlineData("A = 1 or C = 2", "(A = 1 or C = 2) and B = 'x'")]
    public void And_CombinesOrReplaces(string? query, string expected) =>
        Assert.Equal(expected, LogQueryText.And(query, "B = 'x'"));
}