namespace LogVue.Services.Settings;

/// <summary>
/// The minimum level LogVue stores, backed by <see cref="LogVueOptions.LevelSwitch"/> so a change
/// applies to the very next event without a restart.
/// </summary>
public interface IMinimumLevelService
{
    LogEventLevel Current { get; }

    Result Set(LogEventLevel level);
}

public sealed class MinimumLevelService(
    ILogger<MinimumLevelService> logger,
    IOptions<LogVueOptions> options) : IMinimumLevelService
{
    public LogEventLevel Current => options.Value.LevelSwitch.MinimumLevel;

    public Result Set(LogEventLevel level)
    {
        if (options.Value.ReadOnly)
        {
            return Result.Forbidden();
        }

        if (!Enum.IsDefined(level))
        {
            return Result.Invalid(new ValidationError(nameof(level), $"'{level}' is not a log level."));
        }

        var previous = options.Value.LevelSwitch.MinimumLevel;
        options.Value.LevelSwitch.MinimumLevel = level;

        logger.LogInformation("LogVue minimum level changed from {Previous} to {Current}", previous, level);

        return Result.Success();
    }
}
