using Ardalis.Result;
using LogVue.Data;
using LogVue.Data.Npgsql;
using LogVue.Models;
using LogVue.Services;
using Serilog.Events;

namespace LogVue.Tests.Providers;

/// <summary>
/// Runs every query shape the dashboard issues against each database, through its real
/// migrations. Translation, case sensitivity, LIKE escaping, cascades and UTC round-tripping are
/// exactly where providers differ.
/// </summary>
/// <remarks>
/// Each test creates and drops its own database. For example:
/// <code>
/// LOGVUE_TEST_SQLSERVER = Server=(localdb)\MSSQLLocalDB;Trusted_Connection=True;TrustServerCertificate=True
/// LOGVUE_TEST_NPGSQL    = Host=localhost;Username=postgres;Password=...
/// LOGVUE_TEST_MYSQL     = Server=localhost;User ID=root;Password=...
/// </code>
/// </remarks>
public class ProviderTests
{
    private const string SqlServerVariable = "LOGVUE_TEST_SQLSERVER";
    private const string NpgsqlVariable = "LOGVUE_TEST_NPGSQL";
    private const string MySqlVariable = "LOGVUE_TEST_MYSQL";

    [Fact]
    public Task Sqlite_SupportsEveryDashboardOperation() =>
        RunScenarioAsync(TestDatabase.CreateSqlite());

    [ProviderFact(SqlServerVariable)]
    public Task SqlServer_SupportsEveryDashboardOperation() =>
        RunScenarioAsync(TestDatabase.CreateOnServer(
            Environment.GetEnvironmentVariable(SqlServerVariable)!,
            connectionString => new Data.Sql.LogVueDbContextFactory(connectionString),
            new DefaultQueryDialect()));

    [ProviderFact(NpgsqlVariable)]
    public Task Npgsql_SupportsEveryDashboardOperation() =>
        RunScenarioAsync(TestDatabase.CreateOnServer(
            Environment.GetEnvironmentVariable(NpgsqlVariable)!,
            connectionString => new Data.Npgsql.LogVueDbContextFactory(connectionString),
            new NpgsqlQueryDialect()));

    [ProviderFact(MySqlVariable)]
    public Task MySql_SupportsEveryDashboardOperation() =>
        RunScenarioAsync(TestDatabase.CreateOnServer(
            Environment.GetEnvironmentVariable(MySqlVariable)!,
            connectionString => new Data.MySql.LogVueDbContextFactory(connectionString),
            new Data.MySql.MySqlQueryDialect()));

    private static async Task RunScenarioAsync(TestDatabase created)
    {
        using var database = created;
        var service = database.CreateLogEntryService();
        var now = DateTime.UtcNow;

        await database.AddAsync("old", LogEventLevel.Warning, now.AddDays(-40));
        await database.AddAsync("Order 1 placed", LogEventLevel.Information, now.AddMinutes(-3), null,
            ("Customer", "Contoso"), ("SourceContext", "Shop.Api.OrdersController"));
        var payment = await database.AddAsync("Payment TIMEOUT", LogEventLevel.Error, now.AddMinutes(-2), "System.TimeoutException: gateway",
            ("Customer", "Fabrikam"));
        await database.AddAsync("Progress 100% done", LogEventLevel.Debug, now.AddMinutes(-1));
        await database.AddAsync("Progress 1000 done", LogEventLevel.Debug, now);

        async Task<string[]> Search(string? query = null, LogFilter? filter = null)
        {
            var result = await service.SearchAsync(new LogEntrySearchRequest
            {
                Filter = (filter ?? new LogFilter()) with { Query = query },
            });

            Assert.True(result.IsSuccess, $"'{query}' failed: {result.GetDisplayMessage()}");
            return [.. result.Value.Items.Select(e => e.Message)];
        }

        // Keyset paging, newest first.
        var page1 = await service.SearchAsync(new LogEntrySearchRequest { PageSize = 2 });
        Assert.Equal(["Progress 1000 done", "Progress 100% done"], page1.Value.Items.Select(e => e.Message));
        Assert.True(page1.Value.HasMore);

        var page2 = await service.SearchAsync(new LogEntrySearchRequest { PageSize = 2, BeforeId = page1.Value.Items[^1].Id });
        Assert.Equal(["Payment TIMEOUT", "Order 1 placed"], page2.Value.Items.Select(e => e.Message));
        Assert.True(page2.Value.HasMore);

        // Timestamps come back as the same UTC instant.
        var newest = page1.Value.Items[0];
        Assert.Equal(DateTimeKind.Utc, newest.Timestamp.Kind);
        Assert.Equal(now, newest.Timestamp, TimeSpan.FromMilliseconds(1));

        // Text search: case-insensitive, across message, exception and property values, wildcards literal.
        Assert.Equal(["Payment TIMEOUT"], await Search("timeout"));
        Assert.Equal(["Payment TIMEOUT"], await Search("gateway"));
        Assert.Equal(["Order 1 placed"], await Search("CONTOSO"));
        Assert.Equal(["Progress 100% done"], await Search("100%"));

        // Expressions.
        Assert.Equal(["Order 1 placed"], await Search("customer = 'CONTOSO'"));
        Assert.Equal(["Progress 1000 done", "Progress 100% done", "Payment TIMEOUT", "old"], await Search("Customer != 'contoso'"));
        Assert.Equal(["Order 1 placed"], await Search("SourceContext like '%orders%'"));
        Assert.Equal(["Payment TIMEOUT", "old"], await Search("level = error or level = warning"));
        Assert.Equal(["Payment TIMEOUT"], await Search("exception like '%timeoutexception%' and Customer = 'fabrikam'"));
        Assert.Equal(["Progress 1000 done", "Progress 100% done"], await Search("message like 'progress%' and message not like '%order%'"));

        // Structured criteria.
        Assert.Equal(["Payment TIMEOUT"], await Search(filter: new LogFilter { ExceptionsOnly = true }));
        Assert.Equal(["Progress 1000 done", "Progress 100% done"], await Search(filter: new LogFilter
        {
            Levels = [LogEventLevel.Debug],
            FromUtc = now.AddHours(-1),
        }));

        var counts = await service.GetLevelCountsAsync(new LogFilter { FromUtc = now.AddHours(-1) });
        Assert.Equal(2, counts.Value.Single(c => c.Level == LogEventLevel.Debug).Count);
        Assert.Equal(1, counts.Value.Single(c => c.Level == LogEventLevel.Error).Count);
        Assert.Equal(0, counts.Value.Single(c => c.Level == LogEventLevel.Warning).Count);

        var summary = (await service.GetSummaryAsync()).Value;
        Assert.Equal(5, summary.TotalCount);
        Assert.Equal(now.AddDays(-40), summary.OldestUtc!.Value, TimeSpan.FromMilliseconds(1));

        // Deletes, including the cascade to properties.
        Assert.Equal(1, (await service.DeleteOlderThanAsync(now.AddDays(-30))).Value);
        Assert.Equal(1, (await service.CountMatchingAsync(new LogFilter { Query = "Customer = 'contoso'" })).Value);
        Assert.Equal(1, (await service.DeleteMatchingAsync(new LogFilter { Query = "Customer = 'contoso'" })).Value);
        Assert.Equal(1, await database.Properties.LongCountAsync());

        Assert.True((await service.DeleteAsync(payment.Id)).IsSuccess);
        Assert.Equal(ResultStatus.NotFound, (await service.DeleteAsync(payment.Id)).Status);
        Assert.Equal(0, await database.Properties.LongCountAsync());
        Assert.Equal(["Progress 1000 done", "Progress 100% done"], await Search());

        // Every character a LIKE pattern or string literal might treat specially, searched literally.
        await database.AddAsync(@"Tag [beta] at C:\temp\file_1.txt costs 5% ('quoted') !important");
        await database.AddAsync(@"Tag xbetax at C:tempfilex1.txt costs 5 quoted important");
        foreach (string term in new[] { "[beta]", @"C:\temp\file_1", "5%", "('quoted')", "!important" })
        {
            Assert.Equal([@"Tag [beta] at C:\temp\file_1.txt costs 5% ('quoted') !important"], await Search($"\"{term}\""));
        }

        // Saved queries.
        var savedQueries = database.CreateSavedQueryService();
        Assert.True((await savedQueries.SaveAsync(new SaveQueryRequest { Name = "Errors", Query = "level = error" })).IsSuccess);
        Assert.True((await savedQueries.SaveAsync(new SaveQueryRequest { Name = "Errors", Query = "level = fatal" })).IsSuccess);
        Assert.Equal("level = fatal", (await savedQueries.GetAllAsync()).Value.Single().Query);
    }
}