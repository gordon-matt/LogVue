namespace LogVue.Models;

/// <summary>
/// Represents the saved query DTO.
/// </summary>
/// <param name="Id">The identifier.</param>
/// <param name="Name">The name.</param>
/// <param name="Query">The query.</param>
/// <param name="CreatedAt">The created at.</param>
public record SavedQueryDto(int Id, string Name, string Query, DateTime CreatedAt);

/// <summary>Saving under a name that already exists replaces that query.</summary>
public record SaveQueryRequest
{
    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the query.
    /// </summary>
    public string Query { get; init; } = string.Empty;
}