namespace LogVue.Services.SavedQueries;

/// <summary>
/// Defines the contract for the saved query service.
/// </summary>
public interface ISavedQueryService
{
    /// <summary>Every saved query, ordered by name.</summary>
    Task<Result<IReadOnlyList<SavedQueryDto>>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a query, replacing any existing one with the same name. The query must parse, so a
    /// saved query can always be run.
    /// </summary>
    Task<Result<SavedQueryDto>> SaveAsync(SaveQueryRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the value.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result.</returns>
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);
}