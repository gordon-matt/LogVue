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

    /// <summary>
    /// Whether the provider can hold the tables in <see cref="Constants.Schema"/>. Providers where a
    /// schema is a database (MySQL) or does not exist (SQLite) return <see langword="false"/>, and
    /// get <see cref="Constants.TablePrefix"/> on every table name instead.
    /// </summary>
    protected virtual bool SupportsSchemas => true;

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(LogVueDbContextBase).Assembly);

        if (SupportsSchemas)
        {
            builder.HasDefaultSchema(Constants.Schema);
            return;
        }

        builder.Model.SetDefaultSchema(null);

        foreach (var entityType in builder.Model.GetEntityTypes().Where(e => e.BaseType is null))
        {
            if (entityType.GetTableName() is not { } tableName)
            {
                continue;
            }

            entityType.SetSchema(null);
            entityType.SetTableName($"{Constants.TablePrefix}{tableName}");
        }
    }
}