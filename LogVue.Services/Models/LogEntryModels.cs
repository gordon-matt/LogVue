namespace LogVue.Models;

/// <summary>
/// What to show. Every criterion combines with AND, so narrowing the level never silently
/// discards the query beside it.
/// </summary>
public record LogFilter
{
    /// <summary>Free text, or an expression such as <c>StatusCode = 500 and SourceContext like '%Api%'</c>.</summary>
    public string? Query { get; init; }

    /// <summary>Empty means every level.</summary>
    public IReadOnlyCollection<LogVueLevel> Levels { get; init; } = [];

    public bool ExceptionsOnly { get; init; }

    /// <summary>Inclusive.</summary>
    public DateTime? FromUtc { get; init; }

    /// <summary>Exclusive.</summary>
    public DateTime? ToUtc { get; init; }

    public bool IsEmpty =>
        string.IsNullOrWhiteSpace(Query)
        && Levels.Count == 0
        && !ExceptionsOnly
        && FromUtc is null
        && ToUtc is null;
}

/// <summary>
/// A page of entries, addressed by cursor rather than page number. New entries arriving at the
/// top never shift what "the next page" means, which offset paging gets wrong on a live log.
/// </summary>
public record LogEntrySearchRequest
{
    public LogFilter Filter { get; init; } = new();

    /// <summary>Only entries older than this id: the next page down.</summary>
    public long? BeforeId { get; init; }

    /// <summary>Only entries newer than this id: what arrived since the last look.</summary>
    public long? AfterId { get; init; }

    public int PageSize { get; init; } = 100;
}

/// <param name="HasMore">
/// For a <see cref="LogEntrySearchRequest.BeforeId"/> page, whether older entries remain. For an
/// <see cref="LogEntrySearchRequest.AfterId"/> page, whether more arrived than fit, meaning there
/// is a gap between this page and what the caller already has.
/// </param>
public record LogEntryPageDto(IReadOnlyList<LogEntryDto> Items, bool HasMore);

public record LogEntryDto(
    long Id,
    DateTime Timestamp,
    LogVueLevel Level,
    string Message,
    string MessageTemplate,
    string? Exception,
    IReadOnlyList<LogPropertyDto> Properties,
    IReadOnlyList<MessageSegment> Segments)
{
    public string? SourceContext => Properties.FirstOrDefault(p => p.Name == "SourceContext")?.Value;
}

public record LogPropertyDto(string Name, string? Value);

public record LevelCountDto(LogVueLevel Level, long Count);

public record LogStoreSummaryDto(long TotalCount, DateTime? OldestUtc, DateTime? NewestUtc, string ProviderName);

/// <summary>
/// One window of the log store, shaped for the overview. <see cref="PreviousLevels"/> is the same
/// length of time immediately before <see cref="FromUtc"/>, so a tile can say whether errors are
/// rising. It is null for an open-ended window, which has no "previous" to compare with.
/// </summary>
public sealed record LogInsightsDto(
    DateTime FromUtc,
    DateTime ToUtc,
    TimeSpan Bucket,
    IReadOnlyList<LevelCountDto> Levels,
    IReadOnlyList<LevelCountDto>? PreviousLevels,
    long ExceptionCount,
    long? PreviousExceptionCount,
    IReadOnlyList<LogVolumePointDto> Volume,
    IReadOnlyList<LogNameCountDto> TopSources,
    IReadOnlyList<LogNameCountDto> TopTemplates);

/// <summary>Counts per level for one bucket. Levels with no events are present and zero.</summary>
public sealed record LogVolumePointDto(DateTime BucketUtc, IReadOnlyList<LevelCountDto> Levels);

/// <summary>A source, template, or other name, with how often it occurred.</summary>
public sealed record LogNameCountDto(string Name, long Count);