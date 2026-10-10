using Microsoft.EntityFrameworkCore.Design;

namespace LogVue.Data.Npgsql;

/// <summary>
/// Used by <c>dotnet ef</c> so migrations can be generated without a running PostgreSQL server.
/// </summary>
public class LogVueDbContextDesignTimeFactory : IDesignTimeDbContextFactory<LogVueDbContext>
{
    public LogVueDbContext CreateDbContext(string[] args) =>
        new(LogVueDbContextFactory.BuildOptions(
            "Host=localhost;Database=logvue_design;Username=postgres;Password=postgres"));
}