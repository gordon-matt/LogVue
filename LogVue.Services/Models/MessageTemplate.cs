using System.Text;

namespace LogVue.Models;

/// <summary>
/// Splits a message template into literal text and property holes. Covers the subset every
/// structured logger agrees on: <c>{Name}</c>, <c>{Name:format}</c>, alignment, <c>{@Name}</c>,
/// <c>{$Name}</c>, and doubled braces.
/// </summary>
internal static class MessageTemplate
{
    public readonly record struct Token(bool IsProperty, string Text, string? Name, string? Format);

    public static IEnumerable<Token> Tokenize(string template)
    {
        int index = 0;
        var text = new StringBuilder();

        while (index < template.Length)
        {
            char current = template[index];

            if (current == '{')
            {
                if (index + 1 < template.Length && template[index + 1] == '{')
                {
                    text.Append('{');
                    index += 2;
                    continue;
                }

                if (TryReadProperty(template, index, out int next, out string name, out string? format))
                {
                    if (text.Length > 0)
                    {
                        yield return new Token(false, text.ToString(), null, null);
                        text.Clear();
                    }

                    yield return new Token(true, template[index..next], name, format);
                    index = next;
                    continue;
                }
            }
            else if (current == '}' && index + 1 < template.Length && template[index + 1] == '}')
            {
                text.Append('}');
                index += 2;
                continue;
            }

            text.Append(current);
            index++;
        }

        if (text.Length > 0)
        {
            yield return new Token(false, text.ToString(), null, null);
        }
    }

    private static bool TryReadProperty(
        string template,
        int open,
        out int next,
        out string name,
        out string? format)
    {
        next = open;
        name = string.Empty;
        format = null;
        int cursor = open + 1;

        if (cursor < template.Length && template[cursor] is '@' or '$')
        {
            cursor++;
        }

        int nameStart = cursor;

        if (cursor >= template.Length || !IsNameStart(template[cursor]))
        {
            return false;
        }

        cursor++;

        while (cursor < template.Length && IsNamePart(template[cursor]))
        {
            cursor++;
        }

        name = template[nameStart..cursor];

        if (cursor < template.Length && template[cursor] == ',')
        {
            int mark = ++cursor;

            if (cursor < template.Length && template[cursor] == '-')
            {
                cursor++;
            }

            while (cursor < template.Length && char.IsDigit(template[cursor]))
            {
                cursor++;
            }

            if (cursor == mark || (template[mark] == '-' && cursor == mark + 1))
            {
                return false;
            }
        }

        if (cursor < template.Length && template[cursor] == ':')
        {
            int formatStart = ++cursor;

            while (cursor < template.Length && template[cursor] != '}')
            {
                if (template[cursor] == '{')
                {
                    return false;
                }

                cursor++;
            }

            format = template[formatStart..cursor];
        }

        if (cursor >= template.Length || template[cursor] != '}')
        {
            return false;
        }

        next = cursor + 1;
        return true;
    }

    private static bool IsNameStart(char value) => char.IsLetter(value) || value == '_';

    private static bool IsNamePart(char value) => char.IsLetterOrDigit(value) || value == '_';
}
