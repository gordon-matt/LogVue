namespace LogVue.Providers;

/// <summary>
/// Level stored for an event. The integer values match Serilog's <c>LogEventLevel</c> and
/// <c>Microsoft.Extensions.Logging.LogLevel</c> from Trace through Critical, so rows written by
/// either provider stay comparable and existing databases stay valid.
/// </summary>
public enum LogVueLevel
{
    Verbose = 0,
    Debug = 1,
    Information = 2,
    Warning = 3,
    Error = 4,
    Fatal = 5,
}