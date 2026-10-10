namespace LogVue.Services.Query;

internal abstract record QueryNode;

internal enum LogicalOperator
{
    And,
    Or,
}

internal enum ComparisonOperator
{
    Equal,
    NotEqual,
    Like,
    NotLike,
}

internal sealed record LogicalNode(QueryNode Left, LogicalOperator Operator, QueryNode Right) : QueryNode;

/// <summary><c>Field op 'value'</c>. The value is always text, matching how properties are stored.</summary>
internal sealed record ComparisonNode(string Field, ComparisonOperator Operator, string Value, int Position) : QueryNode;

/// <summary>A free-text search across the message, exception and property values.</summary>
internal sealed record TextSearchNode(IReadOnlyList<string> Terms) : QueryNode;