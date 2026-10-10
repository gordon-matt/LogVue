// Derived from Sejil's query scanner, Copyright (C) 2017 Alaa Masoud, Apache License 2.0.

namespace LogVue.Services.Query;

internal sealed class Scanner(string source)
{
    private static readonly Dictionary<string, TokenType> Keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["and"] = TokenType.And,
        ["or"] = TokenType.Or,
        ["like"] = TokenType.Like,
        ["true"] = TokenType.True,
        ["false"] = TokenType.False,
    };

    private readonly List<Token> tokens = [];
    private int start;
    private int current;

    private bool IsAtEnd => current >= source.Length;

    public List<Token> Scan()
    {
        while (!IsAtEnd)
        {
            start = current;
            ScanToken();
        }

        tokens.Add(new Token(TokenType.End, current, string.Empty));
        return tokens;
    }

    private void ScanToken()
    {
        char c = Advance();
        switch (c)
        {
            case '(':
                AddToken(TokenType.OpenParenthesis);
                break;

            case ')':
                AddToken(TokenType.CloseParenthesis);
                break;

            case '=':
                AddToken(TokenType.Equal);
                break;

            case '!' when Match('='):
                AddToken(TokenType.NotEqual);
                break;

            case '\'':
                ReadString();
                break;

            case ' ' or '\t' or '\r' or '\n':
                break;

            case '-' or '.' when char.IsDigit(Peek()):
            case var _ when char.IsDigit(c):
                ReadNumber();
                break;

            case var _ when IsIdentifierStart(c):
                ReadIdentifier();
                break;

            default:
                throw new LogQueryException($"Unexpected character '{c}' at position {current}.");
        }
    }

    /// <summary>
    /// Reads a single-quoted string. A quote inside the string is written <c>\'</c> and a
    /// backslash <c>\\</c>; the value comes back unescaped because it is sent as a parameter.
    /// </summary>
    private void ReadString()
    {
        var value = new System.Text.StringBuilder();

        while (!IsAtEnd && Peek() != '\'')
        {
            char c = Advance();

            if (c == '\\' && Peek() is '\'' or '\\')
            {
                c = Advance();
            }

            value.Append(c);
        }

        if (IsAtEnd)
        {
            throw new LogQueryException($"Unterminated string starting at position {start + 1}.");
        }

        // The closing quote.
        Advance();

        AddToken(TokenType.String, value.ToString());
    }

    private void ReadNumber()
    {
        while (char.IsDigit(Peek()) || (Peek() == '.' && char.IsDigit(PeekNext())))
        {
            Advance();
        }

        // Kept as typed: property values are stored as text, so "1.50" must not become "1.5".
        AddToken(TokenType.Number, source[start..current]);
    }

    private void ReadIdentifier()
    {
        while (IsIdentifierPart(Peek()))
        {
            Advance();
        }

        string text = source[start..current];

        if (!text.Equals("not", StringComparison.OrdinalIgnoreCase))
        {
            AddToken(Keywords.TryGetValue(text, out var keyword) ? keyword : TokenType.Identifier);
            return;
        }

        while (char.IsWhiteSpace(Peek()))
        {
            Advance();
        }

        int wordStart = current;
        while (char.IsLetter(Peek()))
        {
            Advance();
        }

        if (!source[wordStart..current].Equals("like", StringComparison.OrdinalIgnoreCase))
        {
            throw new LogQueryException(
                $"Unexpected input at position {wordStart + 1}: \"not\" can only be used as \"not like\".");
        }

        AddToken(TokenType.NotLike);
    }

    private static bool IsIdentifierStart(char c) => char.IsLetter(c) || c is '_' or '@';

    private static bool IsIdentifierPart(char c) => char.IsLetterOrDigit(c) || c is '_' or '.' or '@';

    private void AddToken(TokenType type, object? value = null) =>
        tokens.Add(new Token(type, start, source[start..current], value));

    private char Advance() => source[current++];

    private char Peek() => IsAtEnd ? '\0' : source[current];

    private char PeekNext() => current + 1 >= source.Length ? '\0' : source[current + 1];

    private bool Match(char expected)
    {
        if (IsAtEnd || source[current] != expected)
        {
            return false;
        }

        current++;
        return true;
    }
}
