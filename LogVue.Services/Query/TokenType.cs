namespace LogVue.Services.Query;

internal enum TokenType
{
    Identifier,
    String,
    Number,

    OpenParenthesis,
    CloseParenthesis,

    And,
    Or,
    Like,
    NotLike,
    True,
    False,

    Equal,
    NotEqual,

    End,
}

internal sealed record Token(TokenType Type, int Position, string Text, object? Value = null);