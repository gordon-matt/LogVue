using System.Globalization;

namespace LogVue.Infrastructure;

/// <summary>
/// Presents UTC timestamps in the browser's time zone, or in UTC. The circuit runs on the server,
/// whose own time zone says nothing about the person looking at the logs.
/// </summary>
public sealed class TimeDisplay
{
    public static TimeDisplay Utc { get; } = new(TimeZoneInfo.Utc, isUtc: true);

    private TimeDisplay(TimeZoneInfo zone, bool isUtc)
    {
        Zone = zone;
        IsUtc = isUtc;
    }

    public TimeZoneInfo Zone { get; }

    public bool IsUtc { get; }

    public string Label => IsUtc ? "UTC" : Zone.Id;

    /// <summary>Falls back to UTC when the browser reports a zone this server doesn't know.</summary>
    public static TimeDisplay ForBrowser(string? timeZoneId)
    {
        if (!string.IsNullOrWhiteSpace(timeZoneId) && TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out var zone))
        {
            return new TimeDisplay(zone, isUtc: false);
        }

        return Utc;
    }

    public DateTime ToDisplay(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zone);

    public DateTime ToUtc(DateTime display) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(display, DateTimeKind.Unspecified), Zone);

    public string Format(DateTime utc) =>
        ToDisplay(utc).ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);

    public string FormatDate(DateTime utc) =>
        ToDisplay(utc).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public string FormatTime(DateTime utc) =>
        ToDisplay(utc).ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);

    public string FormatLong(DateTime utc) =>
        $"{ToDisplay(utc).ToString("dddd, d MMMM yyyy HH:mm:ss.fff", CultureInfo.InvariantCulture)} ({Label})";

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
