using Serilog.Parsing;

namespace LogVue.Models;

public enum MessageSegmentKind
{
    Text,
    String,
    Number,
    Value,
}

/// <summary>
/// A run of a rendered message, tagged so the dashboard can colour property values apart from
/// the template's literal text.
/// </summary>
public sealed record MessageSegment(string Text, MessageSegmentKind Kind, string? PropertyName = null)
{
    private static readonly MessageTemplateParser TemplateParser = new();

    public static IReadOnlyList<MessageSegment> Parse(
        string template,
        IReadOnlyList<LogPropertyDto> properties,
        string fallbackMessage)
    {
        if (string.IsNullOrEmpty(template))
        {
            return [new MessageSegment(fallbackMessage, MessageSegmentKind.Text)];
        }

        var values = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var property in properties)
        {
            values.TryAdd(property.Name, property.Value);
        }

        var segments = new List<MessageSegment>();

        foreach (var token in TemplateParser.Parse(template).Tokens)
        {
            switch (token)
            {
                case TextToken text when text.Text.Length > 0:
                    segments.Add(new MessageSegment(text.Text, MessageSegmentKind.Text));
                    break;

                case PropertyToken property when values.TryGetValue(property.PropertyName, out string? value)
                                                 && !string.IsNullOrEmpty(value):
                    segments.Add(new MessageSegment(ApplyFormat(value, property.Format), Classify(value), property.PropertyName));
                    break;

                case PropertyToken property:
                    segments.Add(new MessageSegment(property.ToString(), MessageSegmentKind.Text));
                    break;
            }
        }

        return segments;
    }

    /// <summary>
    /// Re-applies a template's format specifier, such as <c>{Elapsed:0.00}</c>, to the stored value,
    /// which is kept unformatted so that queries compare against the real number. Uses the current
    /// culture, as Serilog does when it renders the message.
    /// </summary>
    private static string ApplyFormat(string value, string? format)
    {
        if (string.IsNullOrEmpty(format))
        {
            return value;
        }

        try
        {
            if (decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal number))
            {
                return number.ToString(format, CultureInfo.CurrentCulture);
            }

            if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var date))
            {
                return date.ToString(format, CultureInfo.CurrentCulture);
            }
        }
        catch (FormatException)
        {
            // A specifier the type doesn't understand; Serilog falls back to the raw value too.
        }

        return value;
    }

    private static MessageSegmentKind Classify(string value) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _)
            ? MessageSegmentKind.Number
            : value is ['[', .., ']'] or ['{', .., '}'] || value.Contains(" { ", StringComparison.Ordinal)
                ? MessageSegmentKind.Value
                : MessageSegmentKind.String;
}