namespace LogVue.Providers;

/// <summary>
/// The minimum level LogVue stores right now. The dashboard's level picker changes it, and a
/// provider reads <see cref="Current"/> before it maps an event. Subscribe to <see cref="Changed"/>
/// when the provider's own filter has to follow, as Serilog's <c>LoggingLevelSwitch</c> does.
/// </summary>
public interface ILogLevelGate
{
    /// <summary>
    /// Gets the current.
    /// </summary>
    LogVueLevel Current { get; }

    /// <summary>Raised after <see cref="Set"/> changes the level. Not raised when the value is unchanged.</summary>
    event Action<LogVueLevel>? Changed;

    /// <summary>
    /// Sets the value.
    /// </summary>
    /// <param name="level">The level.</param>
    void Set(LogVueLevel level);
}