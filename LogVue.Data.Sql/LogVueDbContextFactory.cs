using Microsoft.Extensions.Logging.Abstractions;

namespace LogVue.Data.Sql;

public class LogVueDbContextFactory(string connectionString) : ILogVueDbContextFactory
{
    private DbContextOptions<LogVueDbContext> Options => field ??= BuildOptions(connectionString);

    public string ProviderName => Constants.DatabaseProviders.SqlServer;

    public DbContext GetContext() => new LogVueDbContext(Options);

    public DbContext GetContext(string connectionString) => new LogVueDbContext(BuildOptions(connectionString));

    internal static DbContextOptions<LogVueDbContext> BuildOptions(string connectionString)
    {
        var optionsBuilder = new DbContextOptionsBuilder<LogVueDbContext>();

        optionsBuilder.UseSqlServer(connectionString, sql => sql
            .MigrationsAssembly(typeof(LogVueDbContext).Assembly.GetName().Name)
            .MigrationsHistoryTable(Constants.MigrationsHistoryTable));

        // LogVue writes through this context. If EF logged its own INSERTs, they would be stored again.
        optionsBuilder.UseLoggerFactory(NullLoggerFactory.Instance);

        return optionsBuilder.Options;
    }
}