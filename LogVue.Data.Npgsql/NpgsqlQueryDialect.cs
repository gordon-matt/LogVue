namespace LogVue.Data.Npgsql;

/// <summary>
/// PostgreSQL compares text case-sensitively under its default collations, so every comparison
/// goes through <c>ILIKE</c>. Equality escapes the value first so it can't act as a pattern.
/// </summary>
public sealed class NpgsqlQueryDialect : DefaultQueryDialect
{
    public override Expression<Func<string?, bool>> EqualTo(string value)
    {
        string pattern = EscapeLikePattern(value);
        string escape = EscapeCharacter;
        return s => EF.Functions.ILike(s!, pattern, escape);
    }

    public override Expression<Func<string?, bool>> Like(string pattern) =>
        s => EF.Functions.ILike(s!, pattern);

    public override Expression<Func<string?, bool>> Contains(string term)
    {
        string pattern = $"%{EscapeLikePattern(term)}%";
        string escape = EscapeCharacter;
        return s => EF.Functions.ILike(s!, pattern, escape);
    }
}
