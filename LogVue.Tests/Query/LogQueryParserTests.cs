using LogVue.Services.Query;

namespace LogVue.Tests.Query;

public class LogQueryParserTests
{
    [Fact]
    public void Parse_PlainWords_IsTextSearchWithDistinctTerms()
    {
        var node = Assert.IsType<TextSearchNode>(LogQueryParser.Parse("timeout  payment Timeout"));

        Assert.Equal(["timeout", "payment"], node.Terms);
    }

    [Fact]
    public void Parse_QuotedPhrase_IsSingleTerm()
    {
        var node = Assert.IsType<TextSearchNode>(LogQueryParser.Parse("\"connection refused\""));

        Assert.Equal(["connection refused"], node.Terms);
    }

    [Fact]
    public void Parse_AndBindsTighterThanOr()
    {
        var node = Assert.IsType<LogicalNode>(LogQueryParser.Parse("a = 1 or b = 2 and c = 3"));

        Assert.Equal(LogicalOperator.Or, node.Operator);
        Assert.Equal("a", Assert.IsType<ComparisonNode>(node.Left).Field);

        var right = Assert.IsType<LogicalNode>(node.Right);
        Assert.Equal(LogicalOperator.And, right.Operator);
    }

    [Fact]
    public void Parse_Parentheses_OverridePrecedence()
    {
        var node = Assert.IsType<LogicalNode>(LogQueryParser.Parse("(a = 1 or b = 2) and c = 3"));

        Assert.Equal(LogicalOperator.And, node.Operator);
        Assert.Equal(LogicalOperator.Or, Assert.IsType<LogicalNode>(node.Left).Operator);
    }

    [Fact]
    public void Parse_KeywordsAreCaseInsensitive()
    {
        var node = Assert.IsType<LogicalNode>(LogQueryParser.Parse("A = 1 AND b NOT LIKE '%x%'"));

        var right = Assert.IsType<ComparisonNode>(node.Right);
        Assert.Equal(ComparisonOperator.NotLike, right.Operator);
        Assert.Equal("%x%", right.Value);
    }

    [Theory]
    [InlineData("level = error", "error")]
    [InlineData("StatusCode = 500", "500")]
    [InlineData("IsAdmin = true", "True")]
    [InlineData(@"Name = 'it\'s'", "it's")]
    [InlineData(@"Path = 'C:\\temp'", @"C:\temp")]
    public void Parse_ComparisonValues(string query, string expected)
    {
        var node = Assert.IsType<ComparisonNode>(LogQueryParser.Parse(query));

        Assert.Equal(expected, node.Value);
    }

    [Theory]
    [InlineData("a = ", "At the end of the query")]
    [InlineData("Name like 5", "like needs a quoted pattern")]
    [InlineData("(a = 1", "expected ')'")]
    [InlineData("a = 1 b = 2", "At position 7")]
    [InlineData("a = 'unterminated", "unterminated")]
    public void Parse_InvalidExpressions_ExplainWhere(string query, string expectedFragment)
    {
        var exception = Assert.Throws<LogQueryException>(() => LogQueryParser.Parse(query));

        Assert.Contains(expectedFragment, exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}