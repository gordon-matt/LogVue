namespace LogVue.Data.MySql;

/// <summary>
/// MySQL reads a backslash inside a string literal as an escape, so the provider's generated
/// <c>ESCAPE '\'</c> is a syntax error there. Any other character works.
/// </summary>
public sealed class MySqlQueryDialect : DefaultQueryDialect
{
    protected override string EscapeCharacter => "!";
}