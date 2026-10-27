namespace LogVue.Providers;

/// <summary>One structured property, already rendered as text.</summary>
/// <param name="Name">Property name, such as <c>SourceContext</c> or <c>OrderId</c>.</param>
/// <param name="Value">Rendered value. Null when the property itself was null.</param>
public readonly record struct LogProperty(string Name, string? Value);

/// <summary>
/// One event handed to <see cref="ILogEventWriter"/>. Providers map their own logger's event
/// into this shape; LogVue stores it and does not care which logger produced it.
/// </summary>
public sealed class LogEventData
{
    /// <summary>When the event occurred. Converted to UTC before it is stored.</summary>
    public required DateTimeOffset Timestamp { get; init; }

    public required LogVueLevel Level { get; init; }

    /// <summary>The message with its template rendered, as it would read in a text log.</summary>
    public required string Message { get; init; }

    /// <summary>The original template, such as <c>Order {OrderId} placed</c>. Empty when the logger has none.</summary>
    public string MessageTemplate { get; init; } = string.Empty;

    /// <summary>The exception rendered with <see cref="Exception.ToString"/>, when there was one.</summary>
    public string? Exception { get; init; }

    public IReadOnlyList<LogProperty> Properties { get; init; } = [];
}
