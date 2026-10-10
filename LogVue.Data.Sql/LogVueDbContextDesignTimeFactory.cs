using Microsoft.EntityFrameworkCore.Design;

namespace LogVue.Data.Sql;

/// <summary>
/// Used by <c>dotnet ef</c> so migrations can be generated without a running SQL Server.
/// </summary>
public class LogVueDbContextDesignTimeFactory : IDesignTimeDbContextFactory<LogVueDbContext>
{
    public LogVueDbContext CreateDbContext(string[] args) =>
        new(LogVueDbContextFactory.BuildOptions(
            "Server=localhost;Database=logvue_design;Trusted_Connection=True;TrustServerCertificate=True"));
}