namespace LogVue.Data.Entities;

public class SavedQuery : BaseEntity<int>
{
    public string Name { get; set; } = string.Empty;

    public string Query { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class SavedQueryMap : IEntityTypeConfiguration<SavedQuery>
{
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