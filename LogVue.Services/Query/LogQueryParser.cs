using System.Text.RegularExpressions;

namespace LogVue.Services.Query;

/// <summary>
/// Decides whether the search box holds a filter expression or plain search text, and parses it.
/// </summary>
internal static partial class LogQueryParser
{
    /// <summary>
    /// Each search term becomes another pass over message, exception and property values, and
    /// nobody types eight useful words into a log search.
    /// </summary>
    private const int MaxSearchTerms = 8;

    public static QueryNode Parse(string query)
    {
        string trimmed = query.Trim();

        if (trimmed.Length >= 2 && trimmed[0] == '"' && trimmed[^1] == '"')
        {
            string phrase = trimmed[1..^1];

            return string.IsNullOrWhiteSpace(phrase)
                ? throw new LogQueryException("The quoted search is empty.")
                : new TextSearchNode([phrase]);
        }

        return !ExpressionHint().IsMatch(trimmed)
            ? new TextSearchNode([.. trimmed
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(MaxSearchTerms)])
            : new Parser(new Scanner(trimmed).Scan()).Parse();
    }

    /// <summary>Whether <paramref name="query"/> would be parsed as an expression rather than searched as text.</summary>
    public static bool IsExpression(string query) => ExpressionHint().IsMatch(query.Trim());

    /// <summary>
    /// A comparison operator or a parenthesis means the user is writing an expression. Anything
    /// else is searched for as text, so typing a word just works.
    /// </summary>
    [GeneratedRegex(@"[=()]|!=|\blike\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ExpressionHint();
}