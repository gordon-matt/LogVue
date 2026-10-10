namespace LogVue.Data.Entities;

/// <summary>
/// One Serilog event. The identity key doubles as the paging cursor: it only ever grows, so
/// "older than id N" is an index seek where Sejil's OFFSET paging re-read every skipped row.
/// </summary>
public class LogEntry : BaseEntity<long>
{
    /// <summary>Always UTC.</summary>
    public DateTime Timestamp { get; set; }

    /// <summary>Stored as an integer, so level filters compare numbers rather than strings.</summary>
    public LogEventLevel Level { get; set; }

    /// <summary>The message with its template rendered, as it would read in a text log.</summary>
    public string Message { get; set; } = string.Empty;

    public string MessageTemplate { get; set; } = string.Empty;

    public string? Exception { get; set; }

    public ICollection<LogEntryProperty> Properties { get; set; } = [];
}

public class LogEntryMap : IEntityTypeConfiguration<LogEntry>
{
    public void Configure(EntityTypeBuilder<LogEntry> builder)
    {
        builder.ToTable(Constants.Tables.Entries);
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id).ValueGeneratedOnAdd();
        builder.Property(m => m.Timestamp).HasConversion(UtcDateTimeConverter.Instance);
        builder.Property(m => m.Level).HasConversion<int>();
        builder.Property(m => m.Message).IsRequired().IsUnicode(true);
        builder.Property(m => m.MessageTemplate).IsRequired().IsUnicode(true);
        builder.Property(m => m.Exception).IsUnicode(true);

        builder.HasMany(m => m.Properties)
            .WithOne(m => m.LogEntry)
            .HasForeignKey(m => m.LogEntryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => m.Timestamp);

        // Level leads so "errors, newest first" is a seek followed by an ordered range read.
        builder.HasIndex(m => new { m.Level, m.Id });
    }
}