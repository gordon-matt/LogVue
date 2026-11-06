namespace LogVue.Data.Sql;

/// <summary>
/// Represents the log vue database context.
/// </summary>
public class LogVueDbContext(DbContextOptions<LogVueDbContext> options) : LogVueDbContextBase(options)
{
}