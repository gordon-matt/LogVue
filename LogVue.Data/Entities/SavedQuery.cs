namespace LogVue.Data.Entities;

/// <summary>
/// Represents the saved query.
/// </summary>
public class SavedQuery : BaseEntity<int>
{
    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the query.
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the created at.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents the saved query map.
/// </summary>
public class SavedQueryMap : IEntityTypeConfiguration<SavedQuery>
{
    /// <summary>
    /// Configures the value.
    /// </summary>
    /// <param name="builder">The builder.</param>
    public void Configure(EntityTypeBuilder<SavedQuery> builder)
    {
        builder.ToTable(Constants.Tables.SavedQueries);
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Name).IsRequired().HasMaxLength(Constants.MaxLengths.SavedQueryName).IsUnicode(true);
        builder.Property(m => m.Query).IsRequired().HasMaxLength(Constants.MaxLengths.SavedQueryText).IsUnicode(true);
        builder.Property(m => m.CreatedAt).HasConversion(UtcDateTimeConverter.Instance);

        builder.HasIndex(m => m.Name).IsUnique();
    }
}