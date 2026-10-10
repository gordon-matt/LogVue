using Microsoft.EntityFrameworkCore.Design;

namespace LogVue.Data.Sqlite;

/// <summary>
/// Used by <c>dotnet ef</c> so migrations can be generated without a host application.
/// </summary>
public class LogVueDbContextDesignTimeFactory : IDesignTimeDbContextFactory<LogVueDbContext>
{
    public LogVueDbContext CreateDbContext(string[] args) =>
        new(LogVueDbContextFactory.BuildOptions("Data Source=logvue_design.db"));
}