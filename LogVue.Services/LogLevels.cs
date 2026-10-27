namespace LogVue.Services;

public static class LogLevels
{
    /// <summary>Most severe first, which is the order the dashboard lists them in.</summary>
    public static IReadOnlyList<LogVueLevel> All { get; } =
    [
        LogVueLevel.Fatal,
        LogVueLevel.Error,
        LogVueLevel.Warning,
        LogVueLevel.Information,
        LogVueLevel.Debug,
        LogVueLevel.Verbose,
    ];

    /// <summary>
    /// Accepts Serilog's names, their Microsoft.Extensions.Logging equivalents and the short
    /// forms people actually type.
    /// </summary>
    public static bool TryParse(string? value, out LogVueLevel level)
    {
        level = default;

        LogVueLevel? parsed = value?.Trim().ToLowerInvariant() switch
        {
            "verbose" or "trace" or "vrb" or "0" => LogVueLevel.Verbose,
            "debug" or "dbg" or "1" => LogVueLevel.Debug,
            "information" or "info" or "inf" or "2" => LogVueLevel.Information,
            "warning" or "warn" or "wrn" or "3" => LogVueLevel.Warning,
            "error" or "err" or "4" => LogVueLevel.Error,
            "fatal" or "critical" or "ftl" or "5" => LogVueLevel.Fatal,
            _ => null,
        };

        if (parsed is null)
        {
            return false;
        }

        level = parsed.Value;
        return true;
    }

    public static string ShortName(LogVueLevel level) => level switch
    {
        LogVueLevel.Verbose => "VRB",
        LogVueLevel.Debug => "DBG",
        LogVueLevel.Information => "INF",
        LogVueLevel.Warning => "WRN",
        LogVueLevel.Error => "ERR",
        LogVueLevel.Fatal => "FTL",
        _ => level.ToString().ToUpperInvariant(),
    };
}