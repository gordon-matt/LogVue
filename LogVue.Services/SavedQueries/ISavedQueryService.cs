namespace LogVue.Services.SavedQueries;

public interface ISavedQueryService
{
    /// <summary>Every saved query, ordered by name.</summary>
    Task<Result<IReadOnlyList<SavedQueryDto>>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a query, replacing any existing one with the same name. The query must parse, so a
    /// saved query can always be run.
    /// </summary>
    Task<Result<SavedQueryDto>> SaveAsync(SaveQueryRequest request, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
