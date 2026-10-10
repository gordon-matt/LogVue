namespace LogVue.Models;

public record SavedQueryDto(int Id, string Name, string Query, DateTime CreatedAt);

/// <summary>Saving under a name that already exists replaces that query.</summary>
public record SaveQueryRequest
{
    public string Name { get; init; } = string.Empty;

    public string Query { get; init; } = string.Empty;
}
