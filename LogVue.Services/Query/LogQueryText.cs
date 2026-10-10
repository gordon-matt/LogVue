namespace LogVue.Services.Query;

/// <summary>
/// Builds query text for the dashboard, so conditions added by clicking always parse.
/// </summary>
public static class LogQueryText
{
    /// <summary>
    /// <c>Name = 'value'</c>, or <c>Name != 'value'</c> when <paramref name="exclude"/> is set,
    /// with quotes and backslashes in the value escaped.
    /// </summary>
    public static string Condition(string field, string? value, bool exclude = false) =>
        $"{field} {(exclude ? "!=" : "=")} {Quote(value ?? string.Empty)}";

    /// <summary>
    /// Narrows <paramref name="query"/> by <paramref name="condition"/>. Plain search text can't be
    /// combined with an expression, so it is replaced rather than producing a query that fails
    /// to parse.
    /// </summary>
    public static string And(string? query, string condition)
    {
        if (string.IsNullOrWhiteSpace(query) || !LogQueryParser.IsExpression(query))
        {
            return condition;
        }

        string trimmed = query.Trim();

        // AND binds tighter than OR, so an OR expression needs parentheses to stay a unit.
        return trimmed.Contains(" or ", StringComparison.OrdinalIgnoreCase)
            ? $"({trimmed}) and {condition}"
            : $"{trimmed} and {condition}";
    }

    public static string Quote(string value) =>
        $"'{value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("'", "\\'", StringComparison.Ordinal)}'";
}