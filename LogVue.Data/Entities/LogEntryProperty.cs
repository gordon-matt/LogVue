namespace LogVue.Data.Entities;

public class LogEntryProperty : BaseEntity<long>
{
    public long LogEntryId { get; set; }

    public LogEntry LogEntry { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The property rendered as text. Scalars are stored without Serilog's surrounding quotes so a
    /// query for <c>SourceContext = 'MyApp.Worker'</c> matches what the user sees.
    /// </summary>
    public string? Value { get; set; }
}

public class LogEntryPropertyMap : IEntityTypeConfiguration<LogEntryProperty>
{
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