namespace LogVue.Data.Entities;

/// <summary>
/// Represents the log entry property.
/// </summary>
public class LogEntryProperty : BaseEntity<long>
{
    /// <summary>
    /// Gets or sets the log entry identifier.
    /// </summary>
    public long LogEntryId { get; set; }

    /// <summary>
    /// Gets or sets the log entry.
    /// </summary>
    public LogEntry LogEntry { get; set; } = null!;

    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The property rendered as text. Scalars are stored without surrounding quotes so a
    /// query for <c>SourceContext = 'MyApp.Worker'</c> matches what the user sees.
    /// </summary>
    public string? Value { get; set; }
}

/// <summary>
/// Represents the log entry property map.
/// </summary>
public class LogEntryPropertyMap : IEntityTypeConfiguration<LogEntryProperty>
{
    /// <summary>
    /// Configures the value.
    /// </summary>
    /// <param name="builder">The builder.</param>
    public void Configure(EntityTypeBuilder<LogEntryProperty> builder)
    {
        builder.ToTable(Constants.Tables.EntryProperties);
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id).ValueGeneratedOnAdd();
        builder.Property(m => m.Name).IsRequired().HasMaxLength(Constants.MaxLengths.PropertyName).IsUnicode(true);
        builder.Property(m => m.Value).IsUnicode(true);

        // Property filters compile to a correlated EXISTS, which this index answers with one seek
        // per candidate entry. Sejil grouped the entire property table on every such query.
        builder.HasIndex(m => new { m.LogEntryId, m.Name });
    }
}