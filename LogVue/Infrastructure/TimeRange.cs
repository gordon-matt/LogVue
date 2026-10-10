namespace LogVue.Infrastructure;

/// <summary>A choice in the time range picker. Relative ranges slide with the clock, so live tail keeps up.</summary>
public sealed record TimeRange(string Key, string Label, TimeSpan? Span)
{
    public const string AllKey = "all";
    public const string CustomKey = "custom";

    public static TimeRange All { get; } = new(AllKey, "All time", null);

    public static TimeRange Custom { get; } = new(CustomKey, "Custom range", null);

    public static IReadOnlyList<TimeRange> Presets { get; } =
    [
        new("15m", "Last 15 minutes", TimeSpan.FromMinutes(15)),
        new("1h", "Last hour", TimeSpan.FromHours(1)),
        new("6h", "Last 6 hours", TimeSpan.FromHours(6)),
        new("24h", "Last 24 hours", TimeSpan.FromHours(24)),
        new("7d", "Last 7 days", TimeSpan.FromDays(7)),
        new("30d", "Last 30 days", TimeSpan.FromDays(30)),
        All,
    ];

    public static TimeRange Find(string? key) =>
        key == CustomKey
            ? Custom
            : Presets.FirstOrDefault(p => string.Equals(p.Key, key, StringComparison.OrdinalIgnoreCase)) ?? All;
}