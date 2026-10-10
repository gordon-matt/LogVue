namespace LogVue.Services;

public static class LogLevels
{
    /// <summary>Most severe first, which is the order the dashboard lists them in.</summary>
    public static IReadOnlyList<LogEventLevel> All { get; } =
    [
        LogEventLevel.Fatal,
        LogEventLevel.Error,
        LogEventLevel.Warning,
        LogEventLevel.Information,
        LogEventLevel.Debug,
        LogEventLevel.Verbose,
    ];

    /// <summary>
    /// Accepts Serilog's names, their Microsoft.Extensions.Logging equivalents and the short
    /// forms people actually type.
    /// </summary>
    public static bool TryParse(string? value, out LogEventLevel level)
    {
        level = default;

        LogEventLevel? parsed = value?.Trim().ToLowerInvariant() switch
        {
            "verbose" or "trace" or "vrb" or "0" => LogEventLevel.Verbose,
            "debug" or "dbg" or "1" => LogEventLevel.Debug,
            "information" or "info" or "inf" or "2" => LogEventLevel.Information,
            "warning" or "warn" or "wrn" or "3" => LogEventLevel.Warning,
            "error" or "err" or "4" => LogEventLevel.Error,
            "fatal" or "critical" or "ftl" or "5" => LogEventLevel.Fatal,
            _ => null,
        };

        if (parsed is null)
        {
            return false;
        }

        level = parsed.Value;
        return true;
    }

    public static string ShortName(LogEventLevel level) => level switch
    {
        LogEventLevel.Verbose => "VRB",
        LogEventLevel.Debug => "DBG",
        LogEventLevel.Information => "INF",
        LogEventLevel.Warning => "WRN",
        LogEventLevel.Error => "ERR",
        LogEventLevel.Fatal => "FTL",
        _ => level.ToString().ToUpperInvariant(),
    };
}