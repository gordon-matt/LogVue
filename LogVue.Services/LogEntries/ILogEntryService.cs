namespace LogVue.Services.LogEntries;

/// <summary>
/// Defines the contract for the log entry service.
/// </summary>
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

    /// <summary>
    /// Gets the summary.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is the outcome for the log store summary.</returns>
    Task<Result<LogStoreSummaryDto>> GetSummaryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts, a time series and the noisiest sources and templates for <paramref name="filter"/>.
    /// The overview's charts are drawn from this rather than from a page of entries.
    /// </summary>
    Task<Result<LogInsightsDto>> GetInsightsAsync(LogFilter filter, CancellationToken cancellationToken = default);

    /// <summary>How many entries <see cref="DeleteMatchingAsync"/> would remove.</summary>
    Task<Result<long>> CountMatchingAsync(LogFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the value.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result.</returns>
    Task<Result> DeleteAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>Deletes every entry matching <paramref name="filter"/>; an empty filter deletes everything.</summary>
    Task<Result<int>> DeleteMatchingAsync(LogFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the older than.
    /// </summary>
    /// <param name="cutoffUtc">The cutoff utc.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is the outcome for the int.</returns>
    Task<Result<int>> DeleteOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default);
}