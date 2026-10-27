using System.Globalization;
using System.Text;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;

namespace LogVue.Providers.Serilog;

/// <summary>
/// Maps a Serilog event into <see cref="LogEventData"/> and queues it. Batching happens in LogVue,
/// so this sink does not wait on the database.
/// </summary>
public sealed class LogVueSerilogSink(ILogEventWriter writer) : ILogEventSink
{
    public void Emit(LogEvent logEvent) => writer.TryWrite(ToEvent(logEvent));

    internal static LogEventData ToEvent(LogEvent logEvent)
    {
        var properties = new List<LogProperty>(logEvent.Properties.Count + 2);

        foreach (var (name, value) in logEvent.Properties)
        {
            properties.Add(new LogProperty(name, RenderValue(value)));
        }

        if (logEvent.TraceId is { } traceId && !logEvent.Properties.ContainsKey(TraceIdProperty))
        {
            properties.Add(new LogProperty(TraceIdProperty, traceId.ToHexString()));
        }

        if (logEvent.SpanId is { } spanId && !logEvent.Properties.ContainsKey(SpanIdProperty))
        {
            properties.Add(new LogProperty(SpanIdProperty, spanId.ToHexString()));
        }

        return new LogEventData
        {
            Timestamp = logEvent.Timestamp,
            Level = LogVueSerilogLevel.FromSerilog(logEvent.Level),
            Message = RenderMessage(logEvent),
            MessageTemplate = logEvent.MessageTemplate.Text,
            Exception = logEvent.Exception?.ToString(),
            Properties = properties,
        };
    }

    private const string TraceIdProperty = "TraceId";
    private const string SpanIdProperty = "SpanId";

    /// <summary>
    /// Serilog's default rendering wraps string values in quotes, which reads as noise in a list
    /// of messages. Strings without a format are written bare; everything else renders normally.
    /// </summary>
    private static string RenderMessage(LogEvent logEvent)
    {
        var builder = new StringBuilder();
        using var writer = new StringWriter(builder, CultureInfo.InvariantCulture);

        foreach (var token in logEvent.MessageTemplate.Tokens)
        {
            if (token is PropertyToken { Format: null } property
                && logEvent.Properties.TryGetValue(property.PropertyName, out var value)
                && value is ScalarValue { Value: string text })
            {
                writer.Write(text);
            }
            else
            {
                token.Render(logEvent.Properties, writer, CultureInfo.InvariantCulture);
            }
        }

        return builder.ToString();
    }

    private static string? RenderValue(LogEventPropertyValue value) => value switch
    {
        ScalarValue { Value: null } => null,
        ScalarValue { Value: string text } => text,
        ScalarValue { Value: DateTime dateTime } => dateTime.ToString("O", CultureInfo.InvariantCulture),
        ScalarValue { Value: DateTimeOffset dateTimeOffset } => dateTimeOffset.ToString("O", CultureInfo.InvariantCulture),
        ScalarValue { Value: IFormattable formattable } => formattable.ToString(null, CultureInfo.InvariantCulture),
        ScalarValue scalar => scalar.Value?.ToString(),
        _ => value.ToString(null, CultureInfo.InvariantCulture),
    };
}