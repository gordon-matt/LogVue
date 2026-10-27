using Heroicsoft.NetForge.Components.Charts;

namespace LogVue.Infrastructure;

/// <summary>Chart.js configurations for the overview. Colours match the level swatches.</summary>
internal static class InsightsCharts
{
    public static object Volume(IReadOnlyList<string> labels, IReadOnlyList<LogVolumePointDto> points)
    {
        var levels = LogLevels.All.Reverse().Where(level => points.Any(p => Count(p, level) > 0)).ToArray();

        return new
        {
            type = "bar",
            data = new
            {
                labels = labels.ToArray(),
                datasets = levels.Select(level => new
                {
                    label = level.ToString(),
                    data = points.Select(p => Count(p, level)).ToArray(),
                    backgroundColor = Color(level),
                    borderWidth = 0,
                    borderRadius = 2,
                }).ToArray(),
            },
            options = new
            {
                responsive = true,
                maintainAspectRatio = false,
                interaction = new { mode = "index", intersect = false },
                scales = new
                {
                    x = new { stacked = true, grid = new { display = false }, ticks = new { maxTicksLimit = 8 } },
                    y = new { stacked = true, beginAtZero = true, ticks = new { precision = 0 } },
                },
                plugins = new
                {
                    legend = new { position = "bottom", labels = new { boxWidth = 12, padding = 12 } },
                    tooltip = new { nfFormat = ChartFormats.Integer },
                },
            },
        };
    }

    public static object Levels(IReadOnlyList<LevelCountDto> levels)
    {
        var present = levels.Where(l => l.Count > 0).ToArray();

        return new
        {
            type = "doughnut",
            data = new
            {
                labels = present.Select(l => l.Level.ToString()).ToArray(),
                datasets = new[]
                {
                    new
                    {
                        data = present.Select(l => l.Count).ToArray(),
                        backgroundColor = present.Select(l => Color(l.Level)).ToArray(),
                        borderWidth = 0,
                    },
                },
            },
            options = new
            {
                responsive = true,
                maintainAspectRatio = false,
                cutout = "62%",
                plugins = new
                {
                    legend = new { position = "right", labels = new { boxWidth = 12, padding = 10 } },
                    tooltip = new { nfFormat = ChartFormats.Integer, nfPercentage = true },
                },
            },
        };
    }

    public static object Names(IReadOnlyList<LogNameCountDto> items)
    {
        return new
        {
            type = "bar",
            data = new
            {
                labels = items.Select(i => Shorten(i.Name)).ToArray(),
                datasets = new[]
                {
                    new
                    {
                        label = "Events",
                        data = items.Select(i => i.Count).ToArray(),
                        backgroundColor = "#4f46e5",
                        borderRadius = 3,
                        borderWidth = 0,
                    },
                },
            },
            options = new
            {
                indexAxis = "y",
                responsive = true,
                maintainAspectRatio = false,
                scales = new
                {
                    x = new { beginAtZero = true, ticks = new { precision = 0 } },
                    y = new { grid = new { display = false } },
                },
                plugins = new
                {
                    legend = new { display = false },
                    tooltip = new { nfFormat = ChartFormats.Integer },
                },
            },
        };
    }

    private static long Count(LogVolumePointDto point, LogEventLevel level) =>
        point.Levels.FirstOrDefault(l => l.Level == level)?.Count ?? 0;

    private static string Shorten(string value)
    {
        string singleLine = value.Replace("\r", " ", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal);
        return singleLine.Length <= 48 ? singleLine : singleLine[..47] + "…";
    }

    private static string Color(LogEventLevel level) => level switch
    {
        LogEventLevel.Verbose => "#94a3b8",
        LogEventLevel.Debug => "#64748b",
        LogEventLevel.Information => "#2563eb",
        LogEventLevel.Warning => "#d97706",
        LogEventLevel.Error => "#dc2626",
        LogEventLevel.Fatal => "#a21caf",
        _ => "#6b7380",
    };
}
