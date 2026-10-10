using Microsoft.EntityFrameworkCore.Design;

namespace LogVue.Data.MySql;

/// <summary>
/// Used by <c>dotnet ef</c> so migrations can be generated without a running MySQL server.
/// </summary>
public class LogVueDbContextDesignTimeFactory : IDesignTimeDbContextFactory<LogVueDbContext>
{
    public LogVueDbContext CreateDbContext(string[] args) =>
        new(LogVueDbContextFactory.BuildOptions(
            "Server=localhost;Database=logvue_design;User=root;Password=root"));
}
