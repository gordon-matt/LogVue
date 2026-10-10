using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;

namespace LogVue.Data.Sqlite;

public class LogVueDbContextFactory(string connectionString) : ILogVueDbContextFactory
{
    private DbContextOptions<LogVueDbContext> Options => field ??= BuildOptions(connectionString);

    public string ProviderName => Constants.DatabaseProviders.Sqlite;

    public DbContext GetContext() => new LogVueDbContext(Options);

    public DbContext GetContext(string connectionString) => new LogVueDbContext(BuildOptions(connectionString));

    internal static DbContextOptions<LogVueDbContext> BuildOptions(string connectionString)
    {
        var optionsBuilder = new DbContextOptionsBuilder<LogVueDbContext>();

        optionsBuilder.UseSqlite(Prepare(connectionString), sqlite => sqlite
            .MigrationsAssembly(typeof(LogVueDbContext).Assembly.GetName().Name)
            .MigrationsHistoryTable(Constants.MigrationsHistoryTable));

        // The sink writes through this context. If EF logged its own INSERTs, Serilog would hand
        // them straight back to the sink.
        optionsBuilder.UseLoggerFactory(NullLoggerFactory.Instance);

        return optionsBuilder.Options;
    }

    /// <summary>
    /// Creates the database's folder, since SQLite will create a missing file but not a missing
    /// directory, and forces foreign keys on so deleting an entry cascades to its properties.
    /// </summary>
    private static string Prepare(string connectionString)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString) { ForeignKeys = true };

        if (builder.DataSource is { Length: > 0 } dataSource
            && !dataSource.Equals(":memory:", StringComparison.OrdinalIgnoreCase)
            && builder.Mode != SqliteOpenMode.Memory
            && Path.GetDirectoryName(Path.GetFullPath(dataSource)) is { Length: > 0 } directory)
        {
            Directory.CreateDirectory(directory);
        }

        return builder.ToString();
    }
}
