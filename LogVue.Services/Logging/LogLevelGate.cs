namespace LogVue.Services.Logging;

/// <summary>
/// Holds the minimum level the dashboard picker changes. Initialised from
/// <see cref="LogVueOptions.MinimumLevel"/> and then updated in place, so a change applies to the
/// next event without rebuilding the logger.
/// </summary>
public sealed class LogLevelGate : ILogLevelGate
{
    private int level;

    /// <summary>
    /// Initializes a new instance of the LogLevelGate class.
    /// </summary>
    /// <param name="options">The options.</param>
    public LogLevelGate(IOptions<LogVueOptions> options)
    {
        level = (int)options.Value.MinimumLevel;
    }

    /// <summary>
    /// Gets the current.
    /// </summary>
    public LogVueLevel Current => (LogVueLevel)Volatile.Read(ref level);

    /// <summary>
    /// Occurs when changed.
    /// </summary>
    public event Action<LogVueLevel>? Changed;

    /// <summary>
    /// Sets the value.
    /// </summary>
    /// <param name="level">The level.</param>
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