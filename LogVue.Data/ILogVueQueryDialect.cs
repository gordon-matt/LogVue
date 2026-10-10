namespace LogVue.Data;

/// <summary>
/// The text comparisons the query language needs, expressed as EF-translatable predicates. They
/// exist as a seam because case-insensitivity is not portable: SQLite, SQL Server and MySQL get
/// it from column collations, while PostgreSQL needs <c>ILIKE</c>.
/// </summary>
public interface ILogVueQueryDialect
{
    /// <summary>Case-insensitive equality.</summary>
    Expression<Func<string?, bool>> EqualTo(string value);

    /// <summary>Case-insensitive <c>LIKE</c> with the caller's own <c>%</c> and <c>_</c> wildcards.</summary>
    Expression<Func<string?, bool>> Like(string pattern);

    /// <summary>Case-insensitive substring match. Wildcard characters in <paramref name="term"/> are literal.</summary>
    Expression<Func<string?, bool>> Contains(string term);
}

/// <summary>
/// Plain <c>=</c> and <c>LIKE</c>, relying on a case-insensitive collation. Correct for SQLite
/// (where the LogVue columns are declared <c>NOCASE</c>), SQL Server and MySQL defaults.
/// </summary>
public class DefaultQueryDialect : ILogVueQueryDialect
{
    /// <summary>The character that makes the next one in a <c>LIKE</c> pattern literal.</summary>
    protected virtual string EscapeCharacter => "\\";

    public virtual Expression<Func<string?, bool>> EqualTo(string value) => s => s == value;

    public virtual Expression<Func<string?, bool>> Like(string pattern) => s => EF.Functions.Like(s, pattern);

    public virtual Expression<Func<string?, bool>> Contains(string term)
    {
        string pattern = $"%{EscapeLikePattern(term)}%";
        string escape = EscapeCharacter;
        return s => EF.Functions.Like(s, pattern, escape);
    }

    /// <summary>
    /// Escapes <c>[</c> as well as <c>%</c> and <c>_</c>, since SQL Server treats a bracket as the
    /// start of a character class. The other providers read an escaped bracket as a plain one.
    /// </summary>
    protected string EscapeLikePattern(string value) => value
        .Replace(EscapeCharacter, EscapeCharacter + EscapeCharacter)
        .Replace("%", EscapeCharacter + "%")
        .Replace("_", EscapeCharacter + "_")
        .Replace("[", EscapeCharacter + "[");
}
