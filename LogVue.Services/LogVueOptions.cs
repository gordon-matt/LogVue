using Serilog.Core;

namespace LogVue;

public sealed class LogVueOptions
{
    public const string SectionName = "LogVue";

    public const string DefaultTitle = "LogVue";

    /// <summary>
    /// Browser tab title. Also shown in the header when <see cref="LogoPath"/> is unset and this
    /// is not <see cref="DefaultTitle"/>.
    /// </summary>
    public string Title { get; set; } = DefaultTitle;

    /// <summary>
    /// Header image. Takes precedence over <see cref="Title"/>; otherwise a custom title is shown,
    /// and the LogVue logo is used for the default title. A root-relative path (<c>/logo.png</c>),
    /// an app-relative path (<c>~/logo.png</c>), or an absolute URL.
    /// </summary>
    public string? LogoPath { get; set; }

    /// <summary>Where the dashboard is mounted. Requests under this path are not logged by LogVue.</summary>
    public string BasePath { get; set; } = "/logvue";

    /// <summary>Entries fetched per scroll step.</summary>
    public int PageSize { get; set; } = 100;

    /// <summary>
    /// Controls which events LogVue stores, and is what the dashboard's level picker changes. Pass
    /// the switch that also controls Serilog's global minimum level to have the picker govern
    /// every sink instead.
    /// </summary>
    public LoggingLevelSwitch LevelSwitch { get; set; } = new(LogEventLevel.Information);

    /// <summary>Hides and refuses deleting entries and changing the minimum level.</summary>
    public bool ReadOnly { get; set; }

    /// <summary>
    /// Applies LogVue's migrations at startup. Turn off when schema changes go through a
    /// deployment pipeline instead; the dashboard then assumes the tables already exist.
    /// </summary>
    public bool AutoMigrate { get; set; } = true;

    /// <summary>Entries older than this many days are deleted automatically. Zero keeps everything.</summary>
    public int RetentionDays { get; set; }

    public TimeSpan RetentionInterval { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Skips events raised while serving the dashboard itself, so browsing the logs doesn't
    /// fill them with requests for the logs.
    /// </summary>
    public bool IgnoreDashboardRequests { get; set; } = true;

    /// <summary>Most events written in one database round trip.</summary>
    public int BatchSizeLimit { get; set; } = 500;

    /// <summary>How long events wait in memory before being written.</summary>
    public TimeSpan BatchPeriod { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>Events held in memory while the database is slow or unavailable. The oldest are dropped beyond this.</summary>
    public int QueueLimit { get; set; } = 100_000;

    /// <summary>How often the dashboard checks for new entries while live tail is on.</summary>
    public TimeSpan LiveRefreshInterval { get; set; } = TimeSpan.FromSeconds(3);

    /// <summary>Applied to deletes, which can touch millions of rows on a large table.</summary>
    public int DeleteCommandTimeoutSeconds { get; set; } = 300;
}