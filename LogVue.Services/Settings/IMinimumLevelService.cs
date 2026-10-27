namespace LogVue.Services.Settings;

/// <summary>
/// The minimum level LogVue stores. A change applies to the next event without a restart.
/// </summary>
public interface IMinimumLevelService
{
    LogVueLevel Current { get; }

    Result Set(LogVueLevel level);
}

public sealed class MinimumLevelService(
    ILogger<MinimumLevelService> logger,
    IOptions<LogVueOptions> options,
    ILogLevelGate levels) : IMinimumLevelService
{
    public LogVueLevel Current => levels.Current;

    public Result Set(LogVueLevel level)
    {
        if (options.Value.ReadOnly)
        {
            return Result.Forbidden();
        }

        if (!Enum.IsDefined(level))
        {
            return Result.Invalid(new ValidationError(nameof(level), $"'{level}' is not a log level."));
        }

        var previous = levels.Current;
        levels.Set(level);

        logger.LogInformation("LogVue minimum level changed from {Previous} to {Current}", previous, level);

        return Result.Success();
    }
}