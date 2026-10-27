using LogVue.Infrastructure;
namespace LogVue.Tests.Infrastructure;

public class DashboardFilterTests
{
    [Fact]
    public void FromUri_ReadsEveryCriterion()
    {
        var filter = DashboardFilter.FromUri(
            "http://localhost/logvue/?q=StatusCode%20%3D%20500&levels=err,wrn,bogus&ex=1&range=custom&from=2026-10-01T00:00:00Z&to=2026-10-02T12:30:00Z");

        Assert.Equal("StatusCode = 500", filter.Query);
        Assert.Equal([LogVueLevel.Warning, LogVueLevel.Error], filter.Levels.Order());
        Assert.True(filter.ExceptionsOnly);
        Assert.Equal(TimeRange.Custom, filter.Range);
        Assert.Equal(new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc), filter.FromUtc);
        Assert.Equal(DateTimeKind.Utc, filter.ToUtc!.Value.Kind);
    }

    [Fact]
    public void FromUri_NoQueryString_IsEmpty()
    {
        var filter = DashboardFilter.FromUri("http://localhost/logvue/");

        Assert.False(filter.HasCriteria);
        Assert.Equal(TimeRange.All, filter.Range);
    }

    [Fact]
    public void ToLogFilter_RelativeRange_ResolvesAgainstNow()
    {
        var now = new DateTime(2026, 10, 10, 12, 0, 0, DateTimeKind.Utc);
        var filter = new DashboardFilter { Range = TimeRange.Find("1h") };

        var logFilter = filter.ToLogFilter(now);

        Assert.Equal(now.AddHours(-1), logFilter.FromUtc);
        Assert.Null(logFilter.ToUtc);
    }

    [Fact]
    public void ToggleLevel_AddsThenRemoves()
    {
        var filter = new DashboardFilter().ToggleLevel(LogVueLevel.Error);
        Assert.Contains(LogVueLevel.Error, filter.Levels);

        Assert.Empty(filter.ToggleLevel(LogVueLevel.Error).Levels);
    }
}