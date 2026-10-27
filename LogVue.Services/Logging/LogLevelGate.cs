using LogVue.Providers;

namespace LogVue.Services.Logging;

/// <summary>
/// Holds the minimum level the dashboard picker changes. Initialised from
/// <see cref="LogVueOptions.MinimumLevel"/> and then updated in place, so a change applies to the
/// next event without rebuilding the logger.
/// </summary>
public sealed class LogLevelGate : ILogLevelGate
{
    private int level;

    public LogLevelGate(IOptions<LogVueOptions> options)
    {
        level = (int)options.Value.MinimumLevel;
    }

    public LogVueLevel Current => (LogVueLevel)Volatile.Read(ref level);

    public event Action<LogVueLevel>? Changed;

    public void Set(LogVueLevel level)
    {
        int updated = (int)level;
        int previous = Interlocked.Exchange(ref this.level, updated);

        if (previous != updated)
        {
            Changed?.Invoke(level);
        }
    }
}
