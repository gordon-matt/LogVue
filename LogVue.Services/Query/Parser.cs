// Derived from Sejil's query parser, Copyright (C) 2017 Alaa Masoud, Apache License 2.0.

namespace LogVue.Services.Query;

/// <summary>
/// Recursive-descent parser for the filter language:
/// <code>
/// or         := and ( "or" and )*
/// and        := primary ( "and" primary )*
/// primary    := "(" or ")" | comparison
/// comparison := identifier ( "=" | "!=" | "like" | "not like" ) ( literal | identifier )
/// </code>
/// </summary>
internal sealed class Parser(List<Token> tokens)
{
    private int current;

    private bool IsAtEnd => Peek().Type == TokenType.End;

    public QueryNode Parse()
    {
        if (IsAtEnd)
        {
            throw new LogQueryException("The query is empty.");
        }

        var node = Or();

        if (!IsAtEnd)
        {
            throw new LogQueryException(Error(Peek(), "expected \"and\", \"or\" or the end of the query."));
        }

        return node;
    }

    private QueryNode Or()
    {
        var node = And();

        while (Match(TokenType.Or))
        {
            node = new LogicalNode(node, LogicalOperator.Or, And());
        }

        return node;
    }

    private QueryNode And()
    {
        var node = Primary();

        while (Match(TokenType.And))
        {
            node = new LogicalNode(node, LogicalOperator.And, Primary());
        }

        return node;
    }

    private QueryNode Primary()
    {
        if (Match(TokenType.OpenParenthesis))
        {
            var node = Or();
            Consume(TokenType.CloseParenthesis, "expected ')'.");
            return node;
        }

        return Comparison();
    }

    private QueryNode Comparison()
    {
        var field = Consume(TokenType.Identifier, "expected a property name such as SourceContext or level.");

        var op = Advance();
        var comparison = op.Type switch
        {
            TokenType.Equal => ComparisonOperator.Equal,
            TokenType.NotEqual => ComparisonOperator.NotEqual,
            TokenType.Like => ComparisonOperator.Like,
            TokenType.NotLike => ComparisonOperator.NotLike,
            _ => throw new LogQueryException(Error(op, "expected =, !=, like or not like.")),
        };

        var literal = Advance();
        string value = literal.Type switch
        {
            TokenType.String => (string)literal.Value!,
            TokenType.Number => (string)literal.Value!,
            TokenType.True => bool.TrueString,
            TokenType.False => bool.FalseString,

            // A bare word reads naturally as a value (level = error, Environment = Production), and
            // there is no property-to-property comparison it could otherwise mean.
            TokenType.Identifier => literal.Text,
            _ => throw new LogQueryException(Error(literal, "expected a value, such as 'text', 42 or true.")),
        };

        if (comparison is ComparisonOperator.Like or ComparisonOperator.NotLike && literal.Type != TokenType.String)
        {
            throw new LogQueryException(Error(literal, "like needs a quoted pattern, such as '%timeout%'."));
        }

        return new ComparisonNode(field.Text, comparison, value, field.Position);
    }

    private bool Match(TokenType type)
    {
        if (Peek().Type != type)
        {
            return false;
        }

        Advance();
        return true;
    }

    private Token Consume(TokenType type, string message) =>
        Peek().Type == type ? Advance() : throw new LogQueryException(Error(Peek(), message));

    private Token Advance()
    {
        var token = Peek();

        if (!IsAtEnd)
        {
            current++;
        }

        return token;
    }

    private Token Peek() => tokens[current];

    private static string Error(Token token, string message) => token.Type == TokenType.End
        ? $"At the end of the query: {message}"
        : $"At position {token.Position + 1} (\"{token.Text}\"): {message}";
}
