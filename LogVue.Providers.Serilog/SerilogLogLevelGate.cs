using LogVue.Providers;
using Serilog.Core;

namespace LogVue.Providers.Serilog;

/// <summary>
/// A level gate backed by a Serilog switch, so the dashboard picker and
/// <c>MinimumLevel.ControlledBy</c> stay on the same value.
/// </summary>
public sealed class SerilogLogLevelGate(LoggingLevelSwitch levelSwitch) : ILogLevelGate
{
    public LogVueLevel Current => LogVueSerilogLevel.FromSerilog(levelSwitch.MinimumLevel);

    public event Action<LogVueLevel>? Changed;

    public void Set(LogVueLevel level)
    {
        var serilogLevel = LogVueSerilogLevel.ToSerilog(level);

        if (levelSwitch.MinimumLevel == serilogLevel)
        {
            return;
        }

        levelSwitch.MinimumLevel = serilogLevel;
        Changed?.Invoke(level);
    }
}
