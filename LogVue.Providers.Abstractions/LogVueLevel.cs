namespace LogVue.Providers;

/// <summary>
/// Level stored for an event. The integer values match Serilog's <c>LogEventLevel</c> and
/// <c>Microsoft.Extensions.Logging.LogLevel</c> from Trace through Critical, so rows written by
/// either provider stay comparable and existing databases stay valid.
/// </summary>
public enum LogVueLevel
{
    /// <summary>
    /// The verbose value.
    /// </summary>
    Verbose = 0,
    /// <summary>
    /// The debug value.
    /// </summary>
    Debug = 1,
    /// <summary>
    /// The information value.
    /// </summary>
    Information = 2,
    /// <summary>
    /// The warning value.
    /// </summary>
    Warning = 3,
    /// <summary>
    /// The error value.
    /// </summary>
    Error = 4,
    /// <summary>
    /// The fatal value.
    /// </summary>
    Fatal = 5,
}