namespace LogVue.Services.SavedQueries;

/// <summary>
/// Represents the saved query service.
/// </summary>
public sealed class SavedQueryService(
    IOptions<LogVueOptions> options,
    ILogVueQueryDialect dialect,
    IRepository<SavedQuery> repository) : ISavedQueryService
{
    /// <summary>
    /// Gets the all.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is the outcome for the saved queries.</returns>
    public async Task<Result<IReadOnlyList<SavedQueryDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var queries = await repository.FindAsync(new SearchOptions<SavedQuery>
        {
            OrderBy = q => q.OrderBy(x => x.Name),
            CancellationToken = cancellationToken,
        });

        return Result<IReadOnlyList<SavedQueryDto>>.Success([.. queries.Select(ToDto)]);
    }

    /// <summary>
    /// Saves the value.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is the outcome for the saved query.</returns>
    public async Task<Result<SavedQueryDto>> SaveAsync(SaveQueryRequest request, CancellationToken cancellationToken = default)
    {
        if (options.Value.ReadOnly)
        {
            return Result<SavedQueryDto>.Forbidden();
        }

        string name = request.Name.Trim();
        string query = request.Query.Trim();

        if (name.Length == 0)
        {
            return Result<SavedQueryDto>.Invalid(new ValidationError(nameof(request.Name), "Give the query a name."));
        }

        if (name.Length > Constants.MaxLengths.SavedQueryName)
        {
            return Result<SavedQueryDto>.Invalid(new ValidationError(
                nameof(request.Name), $"Names can be at most {Constants.MaxLengths.SavedQueryName} characters."));
        }

        if (query.Length == 0)
        {
            return Result<SavedQueryDto>.Invalid(new ValidationError(nameof(request.Query), "There is no query to save."));
        }

        if (query.Length > Constants.MaxLengths.SavedQueryText)
        {
            return Result<SavedQueryDto>.Invalid(new ValidationError(
                nameof(request.Query), $"Queries can be at most {Constants.MaxLengths.SavedQueryText} characters."));
        }

        try
        {
            new LogQueryCompiler(dialect).Compile(query);
        }
        catch (LogQueryException ex)
        {
            return Result<SavedQueryDto>.Invalid(new ValidationError(nameof(request.Query), ex.Message));
        }

        var existing = await repository.FindOneAsync(new SearchOptions<SavedQuery>
        {
            Query = x => x.Name == name,
            CancellationToken = cancellationToken,
        });

        if (existing is not null)
        {
            existing.Query = query;
            await repository.UpdateAsync(existing, ContextOptions.ForCancellationToken(cancellationToken));
            return Result<SavedQueryDto>.Success(ToDto(existing));
        }

        var saved = await repository.InsertAsync(
            new SavedQuery { Name = name, Query = query, CreatedAt = DateTime.UtcNow },
            ContextOptions.ForCancellationToken(cancellationToken));

        return Result<SavedQueryDto>.Success(ToDto(saved));
    }

    /// <summary>
    /// Deletes the value.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result.</returns>
    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (options.Value.ReadOnly)
        {
            return Result.Forbidden();
        }

        int deleted = await repository.DeleteAsync(x => x.Id == id, ContextOptions.ForCancellationToken(cancellationToken));

        return deleted == 0
            ? Result.NotFound($"Saved query {id} was not found.")
            : Result.Success();
    }

    private static SavedQueryDto ToDto(SavedQuery query) => new(query.Id, query.Name, query.Query, query.CreatedAt);
}