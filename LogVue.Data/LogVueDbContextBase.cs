namespace LogVue.Data;

/// <summary>
/// Provider-agnostic context. Each <c>LogVue.Data.{Provider}</c> project subclasses this and owns
/// its own migration history, because EF bakes provider-specific column types into migrations.
/// </summary>
public abstract class LogVueDbContextBase(DbContextOptions options) : DbContext(options)
{
    public DbSet<LogEntry> LogEntries => Set<LogEntry>();

    public DbSet<LogEntryProperty> LogEntryProperties => Set<LogEntryProperty>();

    public DbSet<SavedQuery> SavedQueries => Set<SavedQuery>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(LogVueDbContextBase).Assembly);
    }
}