using Ardalis.Result;
using LogVue.Models;
using Serilog.Events;

namespace LogVue.Tests.Services;

public class LogEntryServiceTests : IDisposable
{
    private readonly TestDatabase database = TestDatabase.CreateSqlite();

    public void Dispose() => database.Dispose();

    [Fact]
    public async Task SearchAsync_PagesNewestFirstByCursor_WithoutGapsOrOverlap()
    {
        for (int i = 1; i <= 25; i++)
        {
            await database.AddAsync($"Message {i}");
        }

        var service = database.CreateLogEntryService();
        var seen = new List<string>();
        long? beforeId = null;
        bool hasMore;

        do
        {
            var page = await service.SearchAsync(new LogEntrySearchRequest { BeforeId = beforeId, PageSize = 10 });
            Assert.True(page.IsSuccess);

            seen.AddRange(page.Value.Items.Select(e => e.Message));
            beforeId = page.Value.Items[^1].Id;
            hasMore = page.Value.HasMore;
        }
        while (hasMore);

        Assert.Equal(Enumerable.Range(1, 25).Reverse().Select(i => $"Message {i}"), seen);
    }

    [Fact]
    public async Task SearchAsync_AfterId_ReturnsOnlyNewerEntries()
    {
        var first = await database.AddAsync("old");
        await database.AddAsync("new 1");
        await database.AddAsync("new 2");

        var result = await database.CreateLogEntryService().SearchAsync(new LogEntrySearchRequest { AfterId = first.Id });

        Assert.Equal(["new 2", "new 1"], result.Value.Items.Select(e => e.Message));
        Assert.False(result.Value.HasMore);
    }

    [Fact]
    public async Task SearchAsync_LoadsEachEntrysOwnProperties()
    {
        await database.AddAsync("a", properties: [("OrderId", "1"), ("Customer", "Contoso")]);
        await database.AddAsync("b", properties: [("OrderId", "2")]);

        var result = await database.CreateLogEntryService().SearchAsync(new LogEntrySearchRequest());

        var b = result.Value.Items.Single(e => e.Message == "b");
        var a = result.Value.Items.Single(e => e.Message == "a");
        Assert.Equal([new LogPropertyDto("OrderId", "2")], b.Properties);
        Assert.Equal(["Customer", "OrderId"], a.Properties.Select(p => p.Name));
    }

    [Fact]
    public async Task SearchAsync_CombinesLevelsExceptionsAndTimeRange()
    {
        var now = DateTime.UtcNow;
        await database.AddAsync("old error", LogEventLevel.Error, now.AddHours(-3), "boom");
        await database.AddAsync("error with exception", LogEventLevel.Error, now, "boom");
        await database.AddAsync("error without exception", LogEventLevel.Error, now);
        await database.AddAsync("warning with exception", LogEventLevel.Warning, now, "boom");
        await database.AddAsync("info with exception", LogEventLevel.Information, now, "boom");

        var result = await database.CreateLogEntryService().SearchAsync(new LogEntrySearchRequest
        {
            Filter = new LogFilter
            {
                Levels = [LogEventLevel.Error, LogEventLevel.Warning],
                ExceptionsOnly = true,
                FromUtc = now.AddHours(-1),
            },
        });

        Assert.Equal(["warning with exception", "error with exception"], result.Value.Items.Select(e => e.Message));
    }

    [Fact]
    public async Task SearchAsync_TextSearch_MatchesMessageExceptionOrPropertyValue_IgnoringCase()
    {
        await database.AddAsync("Payment TIMEOUT");
        await database.AddAsync("Failed", exception: "System.TimeoutException");
        await database.AddAsync("Failed", properties: [("Reason", "gateway timeout")]);
        await database.AddAsync("Unrelated", properties: [("Timeout", "30")]);

        var result = await Search("timeout");

        Assert.Equal(3, result.Value.Items.Count);
        Assert.DoesNotContain(result.Value.Items, e => e.Message == "Unrelated");
    }

    [Fact]
    public async Task SearchAsync_TextSearch_TreatsWildcardCharactersLiterally()
    {
        await database.AddAsync("Progress 100% done");
        await database.AddAsync("Progress 1000 done");
        await database.AddAsync("file_name.txt");
        await database.AddAsync("filexname.txt");

        Assert.Equal(["Progress 100% done"], (await Search("100%")).Value.Items.Select(e => e.Message));
        Assert.Equal(["file_name.txt"], (await Search("file_name")).Value.Items.Select(e => e.Message));
    }

    [Fact]
    public async Task SearchAsync_PropertyNotEqual_IncludesEntriesWithoutTheProperty()
    {
        await database.AddAsync("contoso", properties: [("Customer", "Contoso")]);
        await database.AddAsync("fabrikam", properties: [("Customer", "Fabrikam")]);
        await database.AddAsync("anonymous");

        var result = await Search("Customer != 'contoso'");

        Assert.Equal(["anonymous", "fabrikam"], result.Value.Items.Select(e => e.Message));
    }

    [Fact]
    public async Task SearchAsync_LikeAndLevelFields()
    {
        await database.AddAsync("orders", LogEventLevel.Error, properties: [("SourceContext", "Shop.Api.OrdersController")]);
        await database.AddAsync("orders debug", LogEventLevel.Debug, properties: [("SourceContext", "Shop.Api.OrdersController")]);
        await database.AddAsync("cart", LogEventLevel.Error, properties: [("SourceContext", "Shop.Api.CartController")]);

        var result = await Search("SourceContext like '%orders%' and level != debug");

        Assert.Equal(["orders"], result.Value.Items.Select(e => e.Message));
    }

    [Fact]
    public async Task SearchAsync_InvalidQuery_ReturnsInvalidWithTheParsersMessage()
    {
        var result = await Search("Customer = ");

        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.Contains("end of the query", result.ValidationErrors.Single().ErrorMessage);
    }

    [Fact]
    public async Task GetLevelCountsAsync_IgnoresTheLevelCriterionButAppliesTheRest()
    {
        await database.AddAsync("a", LogEventLevel.Error, exception: "x");
        await database.AddAsync("b", LogEventLevel.Error);
        await database.AddAsync("c", LogEventLevel.Warning, exception: "x");

        var result = await database.CreateLogEntryService().GetLevelCountsAsync(new LogFilter
        {
            Levels = [LogEventLevel.Fatal],
            ExceptionsOnly = true,
        });

        Assert.Equal(6, result.Value.Count);
        Assert.Equal(1, result.Value.Single(c => c.Level == LogEventLevel.Error).Count);
        Assert.Equal(1, result.Value.Single(c => c.Level == LogEventLevel.Warning).Count);
        Assert.Equal(0, result.Value.Single(c => c.Level == LogEventLevel.Fatal).Count);
    }

    [Fact]
    public async Task DeleteMatchingAsync_DeletesMatchingEntriesAndTheirProperties()
    {
        await database.AddAsync("keep", properties: [("Customer", "Fabrikam")]);
        await database.AddAsync("drop 1", properties: [("Customer", "Contoso"), ("OrderId", "1")]);
        await database.AddAsync("drop 2", properties: [("Customer", "Contoso")]);

        var service = database.CreateLogEntryService();

        var count = await service.CountMatchingAsync(new LogFilter { Query = "Customer = 'Contoso'" });
        var deleted = await service.DeleteMatchingAsync(new LogFilter { Query = "Customer = 'Contoso'" });

        Assert.Equal(2, count.Value);
        Assert.Equal(2, deleted.Value);
        Assert.Equal(["keep"], (await service.SearchAsync(new LogEntrySearchRequest())).Value.Items.Select(e => e.Message));
        Assert.Equal(1, await database.Properties.LongCountAsync());
    }

    [Fact]
    public async Task DeleteOlderThanAsync_DeletesOnlyOlderEntries()
    {
        var now = DateTime.UtcNow;
        await database.AddAsync("old", timestamp: now.AddDays(-10));
        await database.AddAsync("new", timestamp: now.AddDays(-1));

        var service = database.CreateLogEntryService();
        var deleted = await service.DeleteOlderThanAsync(now.AddDays(-5));

        Assert.Equal(1, deleted.Value);
        Assert.Equal(["new"], (await service.SearchAsync(new LogEntrySearchRequest())).Value.Items.Select(e => e.Message));
    }

    [Fact]
    public async Task DeleteAsync_MissingEntry_ReturnsNotFound()
    {
        var result = await database.CreateLogEntryService().DeleteAsync(12345);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Deletes_WhenReadOnly_AreForbiddenAndDeleteNothing()
    {
        var entry = await database.AddAsync("protected");
        database.Options.ReadOnly = true;

        var service = database.CreateLogEntryService();

        Assert.Equal(ResultStatus.Forbidden, (await service.DeleteAsync(entry.Id)).Status);
        Assert.Equal(ResultStatus.Forbidden, (await service.DeleteMatchingAsync(new LogFilter())).Status);
        Assert.Equal(ResultStatus.Forbidden, (await service.DeleteOlderThanAsync(DateTime.UtcNow)).Status);
        Assert.Equal(1, await database.Entries.LongCountAsync());
    }

    [Fact]
    public async Task GetSummaryAsync_ReportsCountAndEnds()
    {
        var now = DateTime.UtcNow;
        await database.AddAsync("first", timestamp: now.AddHours(-2));
        await database.AddAsync("last", timestamp: now);

        var summary = (await database.CreateLogEntryService().GetSummaryAsync()).Value;

        Assert.Equal(2, summary.TotalCount);
        Assert.Equal(now.AddHours(-2), summary.OldestUtc!.Value, TimeSpan.FromMilliseconds(1));
        Assert.Equal(now, summary.NewestUtc!.Value, TimeSpan.FromMilliseconds(1));
        Assert.Equal(DateTimeKind.Utc, summary.NewestUtc.Value.Kind);
        Assert.Equal(Constants.DatabaseProviders.Sqlite, summary.ProviderName);
    }

    private Task<Result<LogEntryPageDto>> Search(string query) =>
        database.CreateLogEntryService().SearchAsync(new LogEntrySearchRequest { Filter = new LogFilter { Query = query } });
}
