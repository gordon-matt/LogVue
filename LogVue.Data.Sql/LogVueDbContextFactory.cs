using Microsoft.Extensions.Logging.Abstractions;

namespace LogVue.Data.Sql;

/// <summary>
/// Represents the log vue database context factory.
/// </summary>
public class LogVueDbContextFactory(string connectionString) : ILogVueDbContextFactory
{
    private DbContextOptions<LogVueDbContext> Options => field ??= BuildOptions(connectionString);

    /// <summary>
    /// Gets the provider name.
    /// </summary>
    public string ProviderName => Constants.DatabaseProviders.SqlServer;

    /// <summary>
    /// Gets the context.
    /// </summary>
    /// <returns>The context.</returns>
    public DbContext GetContext() => new LogVueDbContext(Options);

    /// <summary>
    /// Gets the context.
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    /// <returns>The context.</returns>
    public DbContext GetContext(string connectionString) => new LogVueDbContext(BuildOptions(connectionString));

    internal static DbContextOptions<LogVueDbContext> BuildOptions(string connectionString)
    {
        var optionsBuilder = new DbContextOptionsBuilder<LogVueDbContext>();

        optionsBuilder.UseSqlServer(connectionString, sql => sql
            .MigrationsAssembly(typeof(LogVueDbContext).Assembly.GetName().Name)
            .MigrationsHistoryTable(Constants.MigrationsHistoryTable, Constants.Schema));

        // LogVue writes through this context. If EF logged its own INSERTs, they would be stored again.
        optionsBuilder.UseLoggerFactory(NullLoggerFactory.Instance);

        return optionsBuilder.Options;
    }
}