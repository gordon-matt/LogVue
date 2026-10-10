using Microsoft.Extensions.Logging.Abstractions;

namespace LogVue.Data.MySql;

public class LogVueDbContextFactory(string connectionString) : ILogVueDbContextFactory
{
    private DbContextOptions<LogVueDbContext> Options => field ??= BuildOptions(connectionString);

    public string ProviderName => Constants.DatabaseProviders.MySql;

    public DbContext GetContext() => new LogVueDbContext(Options);

    public DbContext GetContext(string connectionString) => new LogVueDbContext(BuildOptions(connectionString));

    internal static DbContextOptions<LogVueDbContext> BuildOptions(string connectionString)
    {
        var optionsBuilder = new DbContextOptionsBuilder<LogVueDbContext>();

        optionsBuilder.UseMySQL(connectionString, mySql => mySql
            .MigrationsAssembly(typeof(LogVueDbContext).Assembly.GetName().Name)
            .MigrationsHistoryTable(Constants.MigrationsHistoryTable));

        // The sink writes through this context. If EF logged its own INSERTs, Serilog would hand
        // them straight back to the sink.
        optionsBuilder.UseLoggerFactory(NullLoggerFactory.Instance);

        return optionsBuilder.Options;
    }
}
