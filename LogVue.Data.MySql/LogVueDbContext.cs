namespace LogVue.Data.MySql;

/// <summary>
/// Represents the log vue database context.
/// </summary>
public class LogVueDbContext(DbContextOptions<LogVueDbContext> options) : LogVueDbContextBase(options)
{
}