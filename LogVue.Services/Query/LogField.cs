namespace LogVue.Services.Query;

/// <summary>
/// Names in a query that refer to a column of the entry rather than one of its properties. The
/// <c>@</c> forms are the CLEF/Seq names, so queries written for those tools carry over.
/// </summary>
internal enum LogField
{
    Property,
    Message,
    MessageTemplate,
    Exception,
    Level,
    Timestamp,
}

internal static class LogFieldExtensions
{
    private static readonly Dictionary<string, LogField> Fields = new(StringComparer.OrdinalIgnoreCase)
    {
        ["message"] = LogField.Message,
        ["@m"] = LogField.Message,
        ["messageTemplate"] = LogField.MessageTemplate,
        ["@mt"] = LogField.MessageTemplate,
        ["exception"] = LogField.Exception,
        ["@x"] = LogField.Exception,
        ["level"] = LogField.Level,
        ["@l"] = LogField.Level,
        ["timestamp"] = LogField.Timestamp,
        ["@t"] = LogField.Timestamp,
    };

    extension(LogField)
    {
        public static LogField Resolve(string name) =>
            Fields.TryGetValue(name, out var field) ? field : LogField.Property;
    }
}