using System.Threading.Channels;
using LogVue.Providers;

namespace LogVue.Services.Logging;

/// <summary>
/// The in-memory queue behind <see cref="ILogEventWriter"/>. A single background reader drains it.
/// </summary>
public sealed class LogEventQueue : ILogEventWriter
{
    private readonly Channel<LogEventData> channel;
    private readonly IOptionsMonitor<LogVueOptions> options;

    public LogEventQueue(IOptionsMonitor<LogVueOptions> options)
    {
        this.options = options;
        int limit = Math.Max(1, options.CurrentValue.QueueLimit);
        channel = Channel.CreateBounded<LogEventData>(new BoundedChannelOptions(limit)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false,
        });
    }

    internal ChannelReader<LogEventData> Reader => channel.Reader;

    public bool TryWrite(LogEventData logEvent)
    {
        if (IsDashboardTraffic(logEvent))
        {
            return true;
        }

        return channel.Writer.TryWrite(logEvent);
    }

    /// <summary>
    /// Requests for the dashboard are logged by the host like any other request. Storing them
    /// fills the log with the act of reading the log.
    /// </summary>
    private bool IsDashboardTraffic(LogEventData logEvent)
    {
        var settings = options.CurrentValue;

        if (!settings.IgnoreDashboardRequests)
        {
            return false;
        }

        foreach (var property in logEvent.Properties)
        {
            if (property.Name is not ("RequestPath" or "Path") || property.Value is not { } path)
            {
                continue;
            }

            if (path.StartsWith(settings.BasePath, StringComparison.OrdinalIgnoreCase)
                && (path.Length == settings.BasePath.Length || path[settings.BasePath.Length] == '/'))
            {
                return true;
            }
        }

        return false;
    }
}
