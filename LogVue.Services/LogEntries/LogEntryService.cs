namespace LogVue.Services.LogEntries;

public sealed class LogEntryService(
    ILogger<LogEntryService> logger,
    IOptions<LogVueOptions> options,
    ILogVueQueryDialect dialect,
    ILogVueDbContextFactory contextFactory,
    IRepository<LogEntry> entryRepository,
    IRepository<LogEntryProperty> propertyRepository) : ILogEntryService
{
    private const int MaxPageSize = 1000;

    public Task<Result<LogEntryPageDto>> SearchAsync(
        LogEntrySearchRequest request,
        CancellationToken cancellationToken = default) =>
        RunAsync("search the log", async () =>
        {
            var built = BuildPredicate(request.Filter, includeLevels: true);

            if (!built.IsSuccess)
            {
                return built.To<LogEntryPageDto>();
            }

            var predicate = built.Value;

            if (request.BeforeId is long beforeId)
            {
                predicate = predicate.And(e => e.Id < beforeId);
            }

            if (request.AfterId is long afterId)
            {
                predicate = predicate.And(e => e.Id > afterId);
            }

            int pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

            using var connection = entryRepository.OpenConnection();

            // One more than asked for answers "is there another page?" without a COUNT over the
            // whole filtered table, which is what makes deep scrolling cheap.
            var entries = await connection
                .Query(predicate)
                .OrderByDescending(e => e.Id)
                .Take(pageSize + 1)
                .ToListAsync(cancellationToken);

            bool hasMore = entries.Count > pageSize;
            if (hasMore)
            {
                entries.RemoveAt(entries.Count - 1);
            }

            var properties = await GetPropertiesAsync(connection, entries, cancellationToken);

            IReadOnlyList<LogEntryDto> items = [.. entries.Select(e => ToDto(e, properties[e.Id]))];

            return Result<LogEntryPageDto>.Success(new LogEntryPageDto(items, hasMore));
        });

    public Task<Result<IReadOnlyList<LevelCountDto>>> GetLevelCountsAsync(
        LogFilter filter,
        CancellationToken cancellationToken = default) =>
        RunAsync("count entries by level", async () =>
        {
            var built = BuildPredicate(filter, includeLevels: false);

            if (!built.IsSuccess)
            {
                return built.To<IReadOnlyList<LevelCountDto>>();
            }

            using var connection = entryRepository.OpenConnection();

            var rows = await connection
                .Query(built.Value)
                .GroupBy(e => e.Level)
                .Select(g => new { Level = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            IReadOnlyList<LevelCountDto> counts =
            [
                .. LogLevels.All.Select(level => new LevelCountDto(
                    level,
                    rows.FirstOrDefault(r => r.Level == level)?.Count ?? 0)),
            ];

            return Result<IReadOnlyList<LevelCountDto>>.Success(counts);
        });

    public Task<Result<LogStoreSummaryDto>> GetSummaryAsync(CancellationToken cancellationToken = default) =>
        RunAsync("summarise the log store", async () =>
        {
            long total = await entryRepository.LongCountAsync(ContextOptions.ForCancellationToken(cancellationToken));

            using var connection = entryRepository.OpenConnection();

            // Ids follow insertion order, so the ends of the primary key are the oldest and
            // newest entries without scanning the timestamp column.
            var oldest = await connection.Query()
                .OrderBy(e => e.Id)
                .Select(e => (DateTime?)e.Timestamp)
                .FirstOrDefaultAsync(cancellationToken);

            var newest = await connection.Query()
                .OrderByDescending(e => e.Id)
                .Select(e => (DateTime?)e.Timestamp)
                .FirstOrDefaultAsync(cancellationToken);

            return Result<LogStoreSummaryDto>.Success(
                new LogStoreSummaryDto(total, oldest, newest, contextFactory.ProviderName));
        });

    public Task<Result<long>> CountMatchingAsync(LogFilter filter, CancellationToken cancellationToken = default) =>
        RunAsync("count matching entries", async () =>
        {
            var built = BuildPredicate(filter, includeLevels: true);

            return built.IsSuccess
                ? Result<long>.Success(await entryRepository.LongCountAsync(
                    built.Value,
                    ContextOptions.ForCancellationToken(cancellationToken)))
                : built.To<long>();
        });

    public async Task<Result> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        if (options.Value.ReadOnly)
        {
            return Result.Forbidden();
        }

        var deleted = await RunAsync("delete an entry", async () =>
            Result<int>.Success(await entryRepository.DeleteAsync(e => e.Id == id, DeleteOptions(cancellationToken))));

        return !deleted.IsSuccess
            ? deleted.ToVoid()
            : deleted.Value == 0
            ? Result.NotFound($"Entry {id} was not found. It may already have been deleted.")
            : Result.Success();
    }

    public async Task<Result<int>> DeleteMatchingAsync(LogFilter filter, CancellationToken cancellationToken = default)
    {
        if (options.Value.ReadOnly)
        {
            return Result<int>.Forbidden();
        }

        var built = BuildPredicate(filter, includeLevels: true);

        return !built.IsSuccess
            ? built.To<int>()
            : await RunAsync("delete matching entries", async () =>
        {
            int deleted = await entryRepository.DeleteAsync(built.Value, DeleteOptions(cancellationToken));

            logger.LogInformation(
                "Deleted {Count} log entries matching {@Filter}",
                deleted,
                new { filter.Query, filter.Levels, filter.ExceptionsOnly, filter.FromUtc, filter.ToUtc });

            return Result<int>.Success(deleted);
        });
    }

    public async Task<Result<int>> DeleteOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default)
    {
        if (options.Value.ReadOnly)
        {
            return Result<int>.Forbidden();
        }

        var cutoff = ToUtc(cutoffUtc);

        return await RunAsync("delete old entries", async () =>
        {
            int deleted = await entryRepository.DeleteAsync(e => e.Timestamp < cutoff, DeleteOptions(cancellationToken));

            logger.LogInformation("Deleted {Count} log entries older than {Cutoff:u}", deleted, cutoff);

            return Result<int>.Success(deleted);
        });
    }

    /// <summary>
    /// Composes the filter into one expression tree so it runs entirely in the database. Returns
    /// <see cref="ResultStatus.Invalid"/> when the query text doesn't parse.
    /// </summary>
    private Result<Expression<Func<LogEntry, bool>>> BuildPredicate(LogFilter filter, bool includeLevels)
    {
        var predicate = PredicateBuilder.New<LogEntry>(true);

        if (filter.FromUtc is DateTime from)
        {
            var fromUtc = ToUtc(from);
            predicate = predicate.And(e => e.Timestamp >= fromUtc);
        }

        if (filter.ToUtc is DateTime to)
        {
            var toUtc = ToUtc(to);
            predicate = predicate.And(e => e.Timestamp < toUtc);
        }

        if (filter.ExceptionsOnly)
        {
            predicate = predicate.And(e => e.Exception != null);
        }

        var levels = filter.Levels.Distinct().ToList();

        // Selecting every level is the same as selecting none, and cheaper to leave out.
        if (includeLevels && levels.Count > 0 && levels.Count < LogLevels.All.Count)
        {
            // An OR chain rather than Contains(): it translates identically on every provider,
            // where a parameterised collection depends on each provider's JSON support.
            var anyLevel = PredicateBuilder.New<LogEntry>(false);

            foreach (var level in levels)
            {
                anyLevel = anyLevel.Or(e => e.Level == level);
            }

            predicate = predicate.And(anyLevel);
        }

        if (!string.IsNullOrWhiteSpace(filter.Query))
        {
            try
            {
                predicate = predicate.And(new LogQueryCompiler(dialect).Compile(filter.Query));
            }
            catch (LogQueryException ex)
            {
                return Result<Expression<Func<LogEntry, bool>>>.Invalid(
                    new ValidationError(nameof(LogFilter.Query), ex.Message));
            }
        }

        Expression<Func<LogEntry, bool>> expression = predicate;

        return Result<Expression<Func<LogEntry, bool>>>.Success(expression.Expand());
    }

    /// <summary>
    /// Loads the properties for a page in a single indexed query. Sejil joined them onto the page
    /// instead, repeating every message and exception once per property.
    /// </summary>
    private async Task<ILookup<long, LogPropertyDto>> GetPropertiesAsync(
        IRepositoryConnection<LogEntry> connection,
        List<LogEntry> entries,
        CancellationToken cancellationToken)
    {
        if (entries.Count == 0)
        {
            return Enumerable.Empty<LogPropertyDto>().ToLookup(_ => 0L);
        }

        var ids = entries.Select(e => e.Id).ToList();

        using var propertyConnection = propertyRepository.UseConnection(connection);

        var rows = await propertyConnection
            .Query(p => ids.Contains(p.LogEntryId))
            .Select(p => new { p.LogEntryId, p.Name, p.Value })
            .ToListAsync(cancellationToken);

        return rows
            .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ToLookup(p => p.LogEntryId, p => new LogPropertyDto(p.Name, p.Value));
    }

    /// <summary>
    /// A database that is down or unreachable is reported as unavailable rather than thrown, so
    /// the dashboard can say so instead of tearing down the circuit.
    /// </summary>
    private async Task<Result<T>> RunAsync<T>(string operation, Func<Task<Result<T>>> action)
    {
        try
        {
            return await action();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "LogVue could not {Operation}", operation);
            return Result<T>.Unavailable($"Could not {operation}: {ex.GetBaseException().Message}");
        }
    }

    private ContextOptions DeleteOptions(CancellationToken cancellationToken) => new()
    {
        CommandTimeout = options.Value.DeleteCommandTimeoutSeconds,
        CancellationToken = cancellationToken,
    };

    private static DateTime ToUtc(DateTime value) => value.Kind == DateTimeKind.Local
        ? value.ToUniversalTime()
        : DateTime.SpecifyKind(value, DateTimeKind.Utc);

    private static LogEntryDto ToDto(LogEntry entry, IEnumerable<LogPropertyDto> properties)
    {
        IReadOnlyList<LogPropertyDto> list = [.. properties];

        return new LogEntryDto(
            entry.Id,
            entry.Timestamp,
            entry.Level,
            entry.Message,
            entry.MessageTemplate,
            entry.Exception,
            list,
            MessageSegment.Parse(entry.MessageTemplate, list, entry.Message));
    }
}