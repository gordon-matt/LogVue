namespace LogVue.Data;

/// <summary>
/// Provider-agnostic context. Each <c>LogVue.Data.{Provider}</c> project subclasses this and owns
/// its own migration history, because EF bakes provider-specific column types into migrations.
/// </summary>
public abstract class LogVueDbContextBase(DbContextOptions options) : DbContext(options)
{
    /// <summary>
    /// Gets the log entries.
    /// </summary>
    public DbSet<LogEntry> LogEntries => Set<LogEntry>();

    /// <summary>
    /// Gets the log entry properties.
    /// </summary>
    public DbSet<LogEntryProperty> LogEntryProperties => Set<LogEntryProperty>();

    /// <summary>
    /// Gets the saved queries.
    /// </summary>
    public DbSet<SavedQuery> SavedQueries => Set<SavedQuery>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(LogVueDbContextBase).Assembly);
    }
}