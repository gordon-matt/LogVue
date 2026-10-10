namespace LogVue.Services.LogEntries;

public interface ILogEntryService
{
    /// <summary>
    /// One page of entries, newest first. Returns <see cref="ResultStatus.Invalid"/> with the
    /// parser's message when the query can't be understood.
    /// </summary>
    Task<Result<LogEntryPageDto>> SearchAsync(
        LogEntrySearchRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Entries per level for everything else in <paramref name="filter"/>. The level criterion
    /// itself is ignored, so the counts beside each level stay put as levels are toggled.
    /// </summary>
    Task<Result<IReadOnlyList<LevelCountDto>>> GetLevelCountsAsync(
        LogFilter filter,
        CancellationToken cancellationToken = default);

    Task<Result<LogStoreSummaryDto>> GetSummaryAsync(CancellationToken cancellationToken = default);

    /// <summary>How many entries <see cref="DeleteMatchingAsync"/> would remove.</summary>
    Task<Result<long>> CountMatchingAsync(LogFilter filter, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>Deletes every entry matching <paramref name="filter"/>; an empty filter deletes everything.</summary>
    Task<Result<int>> DeleteMatchingAsync(LogFilter filter, CancellationToken cancellationToken = default);

    Task<Result<int>> DeleteOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default);
}