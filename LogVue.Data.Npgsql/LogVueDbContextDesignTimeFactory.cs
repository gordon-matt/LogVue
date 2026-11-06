using Microsoft.EntityFrameworkCore.Design;

namespace LogVue.Data.Npgsql;

/// <summary>
/// Used by <c>dotnet ef</c> so migrations can be generated without a running PostgreSQL server.
/// </summary>
public class LogVueDbContextDesignTimeFactory : IDesignTimeDbContextFactory<LogVueDbContext>
{
    /// <summary>
    /// Creates the database context.
    /// </summary>
    /// <param name="args">The args.</param>
    /// <returns>The database context.</returns>
    public LogVueDbContext CreateDbContext(string[] args) =>
        new(LogVueDbContextFactory.BuildOptions(
            "Host=localhost;Database=logvue_design;Username=postgres;Password=postgres"));
}