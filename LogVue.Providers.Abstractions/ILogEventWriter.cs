namespace LogVue.Providers;

/// <summary>
/// Accepts events from a logging provider. Registered by <c>AddLogVue</c>. The call must not block
/// and must not log through <c>ILogger</c>: it runs on the logging pipeline, and a log written
/// from here would re-enter the provider.
/// </summary>
public interface ILogEventWriter
{
    /// <summary>
    /// Queues an event for storage. Returns <c>false</c> only when the event was rejected.
    /// An event for the dashboard itself is accepted and then discarded, so that still returns <c>true</c>.
    /// </summary>
    bool TryWrite(LogEventData logEvent);
}