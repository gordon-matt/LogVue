using Microsoft.EntityFrameworkCore.Design;

namespace LogVue.Data.Sql;

/// <summary>
/// Used by <c>dotnet ef</c> so migrations can be generated without a running SQL Server.
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
            "Server=localhost;Database=logvue_design;Trusted_Connection=True;TrustServerCertificate=True"));
}