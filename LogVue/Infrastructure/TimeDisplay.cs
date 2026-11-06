using System.Globalization;

namespace LogVue.Infrastructure;

/// <summary>
/// Presents UTC timestamps in the browser's time zone, or in UTC. The circuit runs on the server,
/// whose own time zone says nothing about the person looking at the logs.
/// </summary>
public sealed class TimeDisplay
{
    /// <summary>
    /// Gets the utc.
    /// </summary>
    public static TimeDisplay Utc { get; } = new(TimeZoneInfo.Utc, isUtc: true);

    private TimeDisplay(TimeZoneInfo zone, bool isUtc)
    {
        Zone = zone;
        IsUtc = isUtc;
    }

    /// <summary>
    /// Gets the zone.
    /// </summary>
    public TimeZoneInfo Zone { get; }

    /// <summary>
    /// Gets a value indicating whether this instance is utc.
    /// </summary>
    public bool IsUtc { get; }

    /// <summary>
    /// Gets the label.
    /// </summary>
    public string Label => IsUtc ? "UTC" : Zone.Id;

    /// <summary>Falls back to UTC when the browser reports a zone this server doesn't know.</summary>
    public static TimeDisplay ForBrowser(string? timeZoneId) => !string.IsNullOrWhiteSpace(timeZoneId) && TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out var zone)
            ? new TimeDisplay(zone, isUtc: false)
            : Utc;

    /// <summary>
    /// Converts the value to the display.
    /// </summary>
    /// <param name="utc">The utc.</param>
    /// <returns>The display.</returns>
    public DateTime ToDisplay(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zone);

    /// <summary>
    /// Converts the value to the utc.
    /// </summary>
    /// <param name="display">The display.</param>
    /// <returns>The utc.</returns>
    public DateTime ToUtc(DateTime display) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(display, DateTimeKind.Unspecified), Zone);

    /// <summary>
    /// Formats the value.
    /// </summary>
    /// <param name="utc">The utc.</param>
    /// <returns>The string.</returns>
    public string Format(DateTime utc) =>
        ToDisplay(utc).ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);

    /// <summary>
    /// Formats the date.
    /// </summary>
    /// <param name="utc">The utc.</param>
    /// <returns>The date.</returns>
    public string FormatDate(DateTime utc) =>
        ToDisplay(utc).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>
    /// Formats the time.
    /// </summary>
    /// <param name="utc">The utc.</param>
    /// <returns>The time.</returns>
    public string FormatTime(DateTime utc) =>
        ToDisplay(utc).ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);

    /// <summary>
    /// Formats the long.
    /// </summary>
    /// <param name="utc">The utc.</param>
    /// <returns>The long.</returns>
    public string FormatLong(DateTime utc) =>
        $"{ToDisplay(utc).ToString("dddd, d MMMM yyyy HH:mm:ss.fff", CultureInfo.InvariantCulture)} ({Label})";

    /// <summary>
    /// Gets the relative.
    /// </summary>
    /// <param name="utc">The utc.</param>
    /// <param name="nowUtc">The now utc.</param>
    /// <returns>The relative.</returns>
    public static string Relative(DateTime utc, DateTime nowUtc)
    {
        var elapsed = nowUtc - utc;

        return elapsed.TotalSeconds switch
        {
            < 0 => "just now",
            < 60 => $"{(int)elapsed.TotalSeconds}s ago",
            < 3600 => $"{(int)elapsed.TotalMinutes}m ago",
            < 86400 => $"{(int)elapsed.TotalHours}h ago",
            _ => $"{(int)elapsed.TotalDays}d ago",
        };
    }
}