using System.Diagnostics;

namespace LogVue.Services.Logging;

/// <summary>
/// Stores events written with <see cref="ILogger"/>. Any framework registered as a Microsoft
/// logging provider (Serilog, NLog, log4net) passes through here without a package of its own.
/// </summary>
public sealed class LogVueLoggerProvider(
    ILogEventWriter writer,
    ILogLevelGate levels,
    IOptionsMonitor<LogVueOptions> options) : ILoggerProvider, ISupportExternalScope
{
    private volatile IExternalScopeProvider? scopeProvider;

    internal IExternalScopeProvider? ScopeProvider => scopeProvider;

    public void SetScopeProvider(IExternalScopeProvider scopeProvider) => this.scopeProvider = scopeProvider;

    public ILogger CreateLogger(string categoryName) => new LogVueLogger(categoryName, this, writer, levels, options);

    public void Dispose()
    {
    }

    private sealed class LogVueLogger(
        string categoryName,
        LogVueLoggerProvider provider,
        ILogEventWriter writer,
        ILogLevelGate levels,
        IOptionsMonitor<LogVueOptions> options) : ILogger
    {
        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull =>
            provider.ScopeProvider?.Push(state) ?? NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) =>
            options.CurrentValue.UseLoggerProvider
            && logLevel != LogLevel.None
            && (int)logLevel >= (int)levels.Current;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            string message = formatter(state, exception) ?? string.Empty;
            string template = message;
            var properties = new List<LogProperty>();

            Collect(state, properties, captureTemplate: true, ref template);
            provider.ScopeProvider?.ForEachScope(static (scope, collected) =>
            {
                string unused = string.Empty;
                Collect(scope, collected, captureTemplate: false, ref unused);
            }, properties);

            AddIfMissing(properties, "SourceContext", categoryName);

            if (Activity.Current is { } activity)
            {
                AddIfMissing(properties, "TraceId", activity.TraceId.ToHexString());
                AddIfMissing(properties, "SpanId", activity.SpanId.ToHexString());
            }

            writer.TryWrite(new LogEventData
            {
                Timestamp = DateTimeOffset.UtcNow,
                Level = (LogVueLevel)(int)logLevel,
                Message = message,
                MessageTemplate = template,
                Exception = exception?.ToString(),
                Properties = properties,
            });
        }

        private static void Collect(object? state, List<LogProperty> properties, bool captureTemplate, ref string template)
        {
            if (state is IEnumerable<KeyValuePair<string, object>> pairs)
            {
                foreach (var (key, value) in pairs)
                {
                    if (captureTemplate && key == "{OriginalFormat}" && value is string format)
                    {
                        template = format;
                        continue;
                    }

                    AddIfMissing(properties, key, Format(value));
                }
            }
        }

        private static void AddIfMissing(List<LogProperty> properties, string name, string? value)
        {
            if (name.Length == 0 || name == "{OriginalFormat}")
            {
                return;
            }

            foreach (var property in properties)
            {
                if (property.Name == name)
                {
                    return;
                }
            }

            properties.Add(new LogProperty(name, value));
        }

        private static string? Format(object? value) => value switch
        {
            null => null,
            string text => text,
            DateTime dateTime => dateTime.ToString("O", CultureInfo.InvariantCulture),
            DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O", CultureInfo.InvariantCulture),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString(),
        };

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose()
            {
            }
        }
    }
}