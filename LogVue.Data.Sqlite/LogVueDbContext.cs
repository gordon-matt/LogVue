namespace LogVue.Data.Sqlite;

public class LogVueDbContext(DbContextOptions<LogVueDbContext> options) : LogVueDbContextBase(options)
{
    private const string CaseInsensitive = "NOCASE";

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // SQLite's LIKE already ignores case, but '=' follows the column collation. Without NOCASE,
        // "level = 'error'" would match and "SourceContext = 'myapp'" would not.
        builder.Entity<LogEntry>(entity =>
        {
            entity.Property(m => m.Message).UseCollation(CaseInsensitive);
            entity.Property(m => m.MessageTemplate).UseCollation(CaseInsensitive);
            entity.Property(m => m.Exception).UseCollation(CaseInsensitive);
        });

        builder.Entity<LogEntryProperty>(entity =>
        {
            entity.Property(m => m.Name).UseCollation(CaseInsensitive);
            entity.Property(m => m.Value).UseCollation(CaseInsensitive);
        });

        builder.Entity<SavedQuery>().Property(m => m.Name).UseCollation(CaseInsensitive);
    }
}