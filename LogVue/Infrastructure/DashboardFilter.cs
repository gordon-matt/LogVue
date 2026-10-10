using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;

namespace LogVue.Infrastructure;

/// <summary>
/// What the dashboard is showing, mirrored in the address bar so a view can be bookmarked,
/// shared, or returned to with the back button.
/// </summary>
public sealed record DashboardFilter
{
    private const string QueryKey = "q";
    private const string LevelsKey = "levels";
    private const string ExceptionsKey = "ex";
    private const string RangeKey = "range";
    private const string FromKey = "from";
    private const string ToKey = "to";

    public string? Query { get; init; }

    /// <summary>Empty means every level.</summary>
    public IReadOnlySet<LogEventLevel> Levels { get; init; } = new HashSet<LogEventLevel>();

    public bool ExceptionsOnly { get; init; }

    public TimeRange Range { get; init; } = TimeRange.All;

    /// <summary>Only used with <see cref="TimeRange.Custom"/>.</summary>
    public DateTime? FromUtc { get; init; }

    /// <summary>Only used with <see cref="TimeRange.Custom"/>.</summary>
    public DateTime? ToUtc { get; init; }

    public bool HasCriteria =>
        !string.IsNullOrWhiteSpace(Query) || Levels.Count > 0 || ExceptionsOnly || Range != TimeRange.All;

    public DashboardFilter ToggleLevel(LogEventLevel level)
    {
        var levels = new HashSet<LogEventLevel>(Levels);

        if (!levels.Remove(level))
        {
            levels.Add(level);
        }

        return this with { Levels = levels };
    }

    /// <summary>The service-level filter, resolving a relative range against <paramref name="nowUtc"/>.</summary>
    public LogFilter ToLogFilter(DateTime nowUtc) => new()
    {
        Query = string.IsNullOrWhiteSpace(Query) ? null : Query.Trim(),
        Levels = Levels,
        ExceptionsOnly = ExceptionsOnly,
        FromUtc = Range.Span is TimeSpan span ? nowUtc - span : Range == TimeRange.Custom ? FromUtc : null,
        ToUtc = Range == TimeRange.Custom ? ToUtc : null,
    };

    public string ToUri(NavigationManager navigation) => navigation.GetUriWithQueryParameters(
        new Dictionary<string, object?>
        {
            [QueryKey] = string.IsNullOrWhiteSpace(Query) ? null : Query.Trim(),
            [LevelsKey] = Levels.Count == 0
                ? null
                : string.Join(',', LogLevels.All.Where(Levels.Contains).Select(l => LogLevels.ShortName(l).ToLowerInvariant())),
            [ExceptionsKey] = ExceptionsOnly ? "1" : null,
            [RangeKey] = Range == TimeRange.All ? null : Range.Key,
            [FromKey] = Range == TimeRange.Custom ? FormatDate(FromUtc) : null,
            [ToKey] = Range == TimeRange.Custom ? FormatDate(ToUtc) : null,
        });

    public static DashboardFilter FromUri(string uri)
    {
        var query = QueryHelpers.ParseQuery(new Uri(uri).Query);

        string? Get(string key) => query.TryGetValue(key, out var values) ? values.ToString() : null;

        var levels = new HashSet<LogEventLevel>();
        foreach (string part in (Get(LevelsKey) ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            if (LogLevels.TryParse(part, out var level))
            {
                levels.Add(level);
            }
        }

        var range = TimeRange.Find(Get(RangeKey));

        return new DashboardFilter
        {
            Query = Get(QueryKey),
            Levels = levels,
            ExceptionsOnly = Get(ExceptionsKey) is "1" or "true",
            Range = range,
            FromUtc = range == TimeRange.Custom ? ParseDate(Get(FromKey)) : null,
            ToUtc = range == TimeRange.Custom ? ParseDate(Get(ToKey)) : null,
        };
    }

    private static string? FormatDate(DateTime? value) =>
        value?.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);

    private static DateTime? ParseDate(string? value) =>
        DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsed)
            ? parsed
            : null;
}
