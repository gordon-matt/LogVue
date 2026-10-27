namespace LogVue.Services.LogEntries;

public sealed partial class LogEntryService
{
    private const string SourceContextName = "SourceContext";

    private const int TopNameCount = 8;

    /// <summary>Past this many points a chart becomes a solid block, so the bucket widens instead.</summary>
    private const int MaxBuckets = 160;

    public Task<Result<LogInsightsDto>> GetInsightsAsync(
        LogFilter filter,
        CancellationToken cancellationToken = default) =>
        RunAsync("summarise log activity", async () =>
        {
            var built = BuildPredicate(filter, includeLevels: true);

            if (!built.IsSuccess)
            {
                return built.To<LogInsightsDto>();
            }

            using var connection = entryRepository.OpenConnection();

            DateTime to = filter.ToUtc ?? DateTime.UtcNow;
            DateTime from = filter.FromUtc ?? await OldestTimestampAsync(connection, built.Value, to, cancellationToken);

            if (to <= from)
            {
                to = from.AddMinutes(1);
            }

            TimeSpan bucket = ChooseBucket(to - from);

            var levels = await CountLevelsAsync(connection, built.Value, cancellationToken);
            long exceptions = await connection.Query(built.Value).LongCountAsync(e => e.Exception != null, cancellationToken);

            var hits = await LoadHitsAsync(connection, built.Value, bucket, cancellationToken);
            IReadOnlyList<LogVolumePointDto> volume = FillVolume(from, to, bucket, hits);

            var sources = await TopSourcesAsync(connection, built.Value, cancellationToken);
            var templates = await TopTemplatesAsync(connection, built.Value, cancellationToken);

            IReadOnlyList<LevelCountDto>? previousLevels = null;
            long? previousExceptions = null;

            if (PreviousWindow(filter, to) is LogFilter previous)
            {
                var previousBuilt = BuildPredicate(previous, includeLevels: true);

                if (previousBuilt.IsSuccess)
                {
                    previousLevels = await CountLevelsAsync(connection, previousBuilt.Value, cancellationToken);
                    previousExceptions = await connection.Query(previousBuilt.Value)
                        .LongCountAsync(e => e.Exception != null, cancellationToken);
                }
            }

            return Result<LogInsightsDto>.Success(new LogInsightsDto(
                from,
                to,
                bucket,
                levels,
                previousLevels,
                exceptions,
                previousExceptions,
                volume,
                sources,
                templates));
        });

    /// <summary>
    /// An open-ended filter covers the whole store. Charting from year zero would be a flat line,
    /// so the window starts at the oldest event that actually matches.
    /// </summary>
    private static async Task<DateTime> OldestTimestampAsync(
        IRepositoryConnection<LogEntry> connection,
        Expression<Func<LogEntry, bool>> predicate,
        DateTime to,
        CancellationToken cancellationToken)
    {
        var oldest = await connection.Query(predicate)
            .OrderBy(e => e.Timestamp)
            .Select(e => (DateTime?)e.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);

        DateTime from = oldest ?? to.AddHours(-1);

        return from > to ? to.AddHours(-1) : from;
    }

    private static async Task<IReadOnlyList<LevelCountDto>> CountLevelsAsync(
        IRepositoryConnection<LogEntry> connection,
        Expression<Func<LogEntry, bool>> predicate,
        CancellationToken cancellationToken)
    {
        var rows = await connection.Query(predicate)
            .GroupBy(e => e.Level)
            .Select(g => new { Level = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return
        [
            .. LogLevels.All.Select(level => new LevelCountDto(
                level,
                rows.FirstOrDefault(r => r.Level == level)?.Count ?? 0)),
        ];
    }

    /// <summary>
    /// Groups in the database at a grain the bucket can be rolled up from. Date parts translate
    /// on every provider; truncating with integer division does not, so the rollup is done here.
    /// </summary>
    private static async Task<List<VolumeHit>> LoadHitsAsync(
        IRepositoryConnection<LogEntry> connection,
        Expression<Func<LogEntry, bool>> predicate,
        TimeSpan bucket,
        CancellationToken cancellationToken)
    {
        var query = connection.Query(predicate);

        if (bucket < TimeSpan.FromHours(1))
        {
            var rows = await query
                .GroupBy(e => new
                {
                    e.Timestamp.Year,
                    e.Timestamp.Month,
                    e.Timestamp.Day,
                    e.Timestamp.Hour,
                    e.Timestamp.Minute,
                    e.Level,
                })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    g.Key.Day,
                    g.Key.Hour,
                    g.Key.Minute,
                    g.Key.Level,
                    Count = g.Count(),
                })
                .ToListAsync(cancellationToken);

            return rows.ConvertAll(r => new VolumeHit(
                new DateTime(r.Year, r.Month, r.Day, r.Hour, r.Minute, 0, DateTimeKind.Utc),
                r.Level,
                r.Count));
        }

        if (bucket < TimeSpan.FromDays(1))
        {
            var rows = await query
                .GroupBy(e => new
                {
                    e.Timestamp.Year,
                    e.Timestamp.Month,
                    e.Timestamp.Day,
                    e.Timestamp.Hour,
                    e.Level,
                })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    g.Key.Day,
                    g.Key.Hour,
                    g.Key.Level,
                    Count = g.Count(),
                })
                .ToListAsync(cancellationToken);

            return rows.ConvertAll(r => new VolumeHit(
                new DateTime(r.Year, r.Month, r.Day, r.Hour, 0, 0, DateTimeKind.Utc),
                r.Level,
                r.Count));
        }

        var days = await query
            .GroupBy(e => new
            {
                e.Timestamp.Year,
                e.Timestamp.Month,
                e.Timestamp.Day,
                e.Level,
            })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                g.Key.Day,
                g.Key.Level,
                Count = g.Count(),
            })
            .ToListAsync(cancellationToken);

        return days.ConvertAll(r => new VolumeHit(
            new DateTime(r.Year, r.Month, r.Day, 0, 0, 0, DateTimeKind.Utc),
            r.Level,
            r.Count));
    }

    private static async Task<IReadOnlyList<LogNameCountDto>> TopSourcesAsync(
        IRepositoryConnection<LogEntry> connection,
        Expression<Func<LogEntry, bool>> predicate,
        CancellationToken cancellationToken)
    {
        var rows = await connection.Query(predicate)
            .SelectMany(e => e.Properties)
            .Where(p => p.Name == SourceContextName && p.Value != null && p.Value != "")
            .GroupBy(p => p.Value!)
            .Select(g => new { Name = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Name)
            .Take(TopNameCount)
            .ToListAsync(cancellationToken);

        return rows.ConvertAll(r => new LogNameCountDto(r.Name, r.Count));
    }

    private static async Task<IReadOnlyList<LogNameCountDto>> TopTemplatesAsync(
        IRepositoryConnection<LogEntry> connection,
        Expression<Func<LogEntry, bool>> predicate,
        CancellationToken cancellationToken)
    {
        var rows = await connection.Query(predicate)
            .Where(e => e.MessageTemplate != "")
            .GroupBy(e => e.MessageTemplate)
            .Select(g => new { Name = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Name)
            .Take(TopNameCount)
            .ToListAsync(cancellationToken);

        return rows.ConvertAll(r => new LogNameCountDto(r.Name, r.Count));
    }

    private static IReadOnlyList<LogVolumePointDto> FillVolume(
        DateTime from,
        DateTime to,
        TimeSpan bucket,
        List<VolumeHit> hits)
    {
        var totals = new Dictionary<DateTime, Dictionary<LogEventLevel, long>>();

        foreach (var hit in hits)
        {
            DateTime aligned = Align(hit.Timestamp, bucket);

            if (!totals.TryGetValue(aligned, out var levels))
            {
                levels = [];
                totals[aligned] = levels;
            }

            levels[hit.Level] = levels.GetValueOrDefault(hit.Level) + hit.Count;
        }

        var points = new List<LogVolumePointDto>();

        for (DateTime cursor = Align(from, bucket); cursor < to && points.Count < MaxBuckets; cursor = Next(cursor, bucket))
        {
            totals.TryGetValue(cursor, out var levels);

            points.Add(new LogVolumePointDto(cursor,
            [
                .. LogLevels.All.Select(level => new LevelCountDto(level, levels?.GetValueOrDefault(level) ?? 0)),
            ]));
        }

        if (points.Count == 0)
        {
            points.Add(new LogVolumePointDto(Align(from, bucket),
            [
                .. LogLevels.All.Select(level => new LevelCountDto(level, 0L)),
            ]));
        }

        return points;
    }

    /// <summary>The same duration, ending where <paramref name="filter"/> starts.</summary>
    private static LogFilter? PreviousWindow(LogFilter filter, DateTime toUtc)
    {
        if (filter.FromUtc is not DateTime from)
        {
            return null;
        }

        DateTime to = filter.ToUtc ?? toUtc;

        if (to <= from)
        {
            return null;
        }

        return filter with { FromUtc = from - (to - from), ToUtc = from };
    }

    /// <summary>Widens until the window fits on one chart. A month is the coarsest bucket.</summary>
    private static TimeSpan ChooseBucket(TimeSpan span)
    {
        if (span <= TimeSpan.Zero)
        {
            return TimeSpan.FromMinutes(1);
        }

        TimeSpan[] sizes =
        [
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(15),
            TimeSpan.FromHours(1),
            TimeSpan.FromHours(6),
            TimeSpan.FromDays(1),
            TimeSpan.FromDays(7),
            TimeSpan.FromDays(30),
        ];

        foreach (var size in sizes)
        {
            if (span.Ticks / size.Ticks <= MaxBuckets)
            {
                return size;
            }
        }

        return TimeSpan.FromDays(30);
    }

    private static DateTime Align(DateTime utc, TimeSpan bucket)
    {
        utc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);

        if (bucket >= TimeSpan.FromDays(28))
        {
            return new DateTime(utc.Year, utc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        }

        if (bucket >= TimeSpan.FromDays(7))
        {
            int daysFromMonday = ((int)utc.DayOfWeek + 6) % 7;
            return DateTime.SpecifyKind(utc.Date.AddDays(-daysFromMonday), DateTimeKind.Utc);
        }

        if (bucket >= TimeSpan.FromDays(1))
        {
            return DateTime.SpecifyKind(utc.Date, DateTimeKind.Utc);
        }

        if (bucket >= TimeSpan.FromHours(1))
        {
            int hours = Math.Max(1, (int)bucket.TotalHours);
            int hour = (utc.Hour / hours) * hours;
            return new DateTime(utc.Year, utc.Month, utc.Day, hour, 0, 0, DateTimeKind.Utc);
        }

        int minutes = Math.Max(1, (int)bucket.TotalMinutes);
        int minute = (utc.Minute / minutes) * minutes;
        return new DateTime(utc.Year, utc.Month, utc.Day, utc.Hour, minute, 0, DateTimeKind.Utc);
    }

    private static DateTime Next(DateTime cursor, TimeSpan bucket) =>
        bucket >= TimeSpan.FromDays(28) ? cursor.AddMonths(1) : cursor.Add(bucket);

    private sealed record VolumeHit(DateTime Timestamp, LogEventLevel Level, int Count);
}
