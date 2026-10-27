using LogVue.Models;
using LogVue.Services;
using LogVue.Services.LogEntries;
using Serilog.Events;

namespace LogVue.Tests.Services;

public class LogInsightsTests : IDisposable
{
    private readonly TestDatabase database = TestDatabase.CreateSqlite();

    public void Dispose() => database.Dispose();

    [Fact]
    public async Task GetInsightsAsync_CountsLevelsExceptionsSourcesAndThePreviousWindow()
    {
        var start = new DateTime(2026, 10, 27, 12, 0, 0, DateTimeKind.Utc);
        await database.AddAsync("Orders placed", LogEventLevel.Information, start.AddMinutes(1),
            properties: [("SourceContext", "Shop.Orders")]);
        await database.AddAsync("Orders placed", LogEventLevel.Information, start.AddMinutes(2),
            properties: [("SourceContext", "Shop.Orders")]);
        await database.AddAsync("Cart updated", LogEventLevel.Warning, start.AddMinutes(3), "boom",
            ("SourceContext", "Shop.Cart"));
        await database.AddAsync("earlier failure", LogEventLevel.Error, start.AddMinutes(-4), "boom");
        await database.AddAsync("too old", LogEventLevel.Fatal, start.AddHours(-2));

        var result = await database.CreateLogEntryService().GetInsightsAsync(new LogFilter
        {
            FromUtc = start,
            ToUtc = start.AddMinutes(10),
        });

        Assert.True(result.IsSuccess, result.GetDisplayMessage());
        var insights = result.Value;

        Assert.Equal(2, insights.Levels.Single(l => l.Level == LogEventLevel.Information).Count);
        Assert.Equal(1, insights.Levels.Single(l => l.Level == LogEventLevel.Warning).Count);
        Assert.Equal(0, insights.Levels.Single(l => l.Level == LogEventLevel.Error).Count);
        Assert.Equal(1, insights.ExceptionCount);

        IReadOnlyList<LevelCountDto> previous = insights.PreviousLevels!;
        Assert.Equal(1, previous.Single(l => l.Level == LogEventLevel.Error).Count);
        Assert.Equal(0, previous.Single(l => l.Level == LogEventLevel.Fatal).Count);
        Assert.Equal(1, insights.PreviousExceptionCount);

        Assert.Equal("Shop.Orders", insights.TopSources[0].Name);
        Assert.Equal(2, insights.TopSources[0].Count);
        Assert.Equal(["Shop.Orders", "Shop.Cart"], insights.TopSources.Select(s => s.Name));

        Assert.Equal("Orders placed", insights.TopTemplates[0].Name);
        Assert.Equal(2, insights.TopTemplates[0].Count);
    }

    [Fact]
    public async Task GetInsightsAsync_FillsEmptyBucketsSoTheChartDoesNotSkipTime()
    {
        var start = new DateTime(2026, 10, 27, 12, 0, 0, DateTimeKind.Utc);
        await database.AddAsync("a", LogEventLevel.Error, start.AddMinutes(1));
        await database.AddAsync("b", LogEventLevel.Error, start.AddMinutes(1).AddSeconds(20));
        await database.AddAsync("c", LogEventLevel.Warning, start.AddMinutes(4));

        var insights = (await database.CreateLogEntryService().GetInsightsAsync(new LogFilter
        {
            FromUtc = start,
            ToUtc = start.AddMinutes(10),
        })).Value;

        Assert.Equal(TimeSpan.FromMinutes(1), insights.Bucket);
        Assert.Equal(10, insights.Volume.Count);
        Assert.Equal(start, insights.Volume[0].BucketUtc);

        var busy = insights.Volume.Single(p => p.BucketUtc == start.AddMinutes(1));
        Assert.Equal(2, busy.Levels.Single(l => l.Level == LogEventLevel.Error).Count);

        var gap = insights.Volume.Single(p => p.BucketUtc == start.AddMinutes(2));
        Assert.Equal(0, gap.Levels.Sum(l => l.Count));

        var warning = insights.Volume.Single(p => p.BucketUtc == start.AddMinutes(4));
        Assert.Equal(1, warning.Levels.Single(l => l.Level == LogEventLevel.Warning).Count);
    }

    [Fact]
    public async Task GetInsightsAsync_OpenEndedFilter_StartsAtTheOldestEvent()
    {
        var now = DateTime.UtcNow;
        await database.AddAsync("first", timestamp: now.AddHours(-5));
        await database.AddAsync("second", timestamp: now.AddHours(-1));

        var insights = (await database.CreateLogEntryService().GetInsightsAsync(new LogFilter())).Value;

        Assert.Equal(now.AddHours(-5), insights.FromUtc, TimeSpan.FromMilliseconds(1));
        Assert.Null(insights.PreviousLevels);
        Assert.Equal(2, insights.Levels.Sum(l => l.Count));
    }
}
