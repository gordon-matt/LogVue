using System.Text;
using LogVue.Services.Hosting;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Parsing;

namespace LogVue.Services.Logging;

/// <summary>
/// Writes batches of Serilog events through LogVue's repository. Batching, back-pressure and
/// retry come from Serilog's own <c>BatchingOptions</c>, so this class only maps and inserts.
/// </summary>
public sealed class LogVueSink(
    IServiceScopeFactory scopeFactory,
    IOptions<LogVueOptions> options,
    LogVueDatabaseState databaseState) : IBatchedLogEventSink
{
    /// <summary>
    /// Serilog retries a batch that throws, so timing out here simply defers the batch until
    /// migrations have had longer to finish.
    /// </summary>
    private static readonly TimeSpan ReadyTimeout = TimeSpan.FromSeconds(30);

    public async Task EmitBatchAsync(IReadOnlyCollection<LogEvent> batch)
    {
        if (!await databaseState.Ready.WaitAsync(ReadyTimeout))
        {
            SelfLog.WriteLine("LogVue dropped {0} events because its database could not be prepared.", batch.Count);
            return;
        }

        var entries = batch
            .Where(e => !IsDashboardTraffic(e))
            .Select(ToEntry)
            .ToList();

        if (entries.Count == 0)
        {
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRepository<LogEntry>>();

        await repository.InsertAsync(entries);
    }

    public Task OnEmptyBatchAsync() => Task.CompletedTask;

    internal static LogEntry ToEntry(LogEvent logEvent)
    {
        var entry = new LogEntry
        {
            Timestamp = logEvent.Timestamp.UtcDateTime,
            Level = logEvent.Level,
            Message = RenderMessage(logEvent),
            MessageTemplate = logEvent.MessageTemplate.Text,
            Exception = logEvent.Exception?.ToString(),
        };

        foreach (var (name, value) in logEvent.Properties)
        {
            entry.Properties.Add(new LogEntryProperty
            {
                Name = name.Length <= Constants.MaxLengths.PropertyName ? name : name[..Constants.MaxLengths.PropertyName],
                Value = RenderValue(value),
            });
        }

        if (logEvent.TraceId is { } traceId && !logEvent.Properties.ContainsKey(TraceIdProperty))
        {
            entry.Properties.Add(new LogEntryProperty { Name = TraceIdProperty, Value = traceId.ToHexString() });
        }

        if (logEvent.SpanId is { } spanId && !logEvent.Properties.ContainsKey(SpanIdProperty))
        {
            entry.Properties.Add(new LogEntryProperty { Name = SpanIdProperty, Value = spanId.ToHexString() });
        }

        return entry;
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
        ScalarValue scalar => scalar.Value.ToString(),
        _ => value.ToString(null, CultureInfo.InvariantCulture),
    };

    private bool IsDashboardTraffic(LogEvent logEvent)
    {
        var settings = options.Value;

        if (!settings.IgnoreDashboardRequests)
        {
            return false;
        }

        return IsUnderBasePath("RequestPath") || IsUnderBasePath("Path");

        bool IsUnderBasePath(string propertyName) =>
            logEvent.Properties.TryGetValue(propertyName, out var value)
            && value is ScalarValue { Value: string path }
            && path.StartsWith(settings.BasePath, StringComparison.OrdinalIgnoreCase)
            && (path.Length == settings.BasePath.Length || path[settings.BasePath.Length] == '/');
    }
}
