using LogVue.Providers;
using LogVue.Providers.Serilog;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Configuration;
using Serilog.Core;

// In Serilog's namespace, as sinks conventionally are, so `using Serilog;` is all a host needs.
namespace Serilog;

public static class LogVueLoggerSinkConfigurationExtensions
{
    extension(LoggerSinkConfiguration writeTo)
    {
        /// <summary>
        /// Writes events to LogVue's database. Call from a Serilog configuration that has access to
        /// the application's services:
        /// <code>
        /// builder.Services.AddSerilog((services, logger) => logger
        ///     .MinimumLevel.ControlledBy(levelSwitch)
        ///     .WriteTo.LogVue(services, levelSwitch));
        /// </code>
        /// The switch is optional. Without it, the sink follows the dashboard's level gate.
        /// </summary>
        public LoggerConfiguration LogVue(IServiceProvider services, LoggingLevelSwitch? levelSwitch = null)
        {
            var gate = services.GetRequiredService<ILogLevelGate>();
            var sinkSwitch = Bind(gate, levelSwitch);

            return writeTo.Sink(
                new LogVueSerilogSink(services.GetRequiredService<ILogEventWriter>()),
                levelSwitch: sinkSwitch);
        }
    }

    private static LoggingLevelSwitch Bind(ILogLevelGate gate, LoggingLevelSwitch? levelSwitch)
    {
        if (levelSwitch is null)
        {
            var created = new LoggingLevelSwitch(LogVueSerilogLevel.ToSerilog(gate.Current));
            gate.Changed += level => created.MinimumLevel = LogVueSerilogLevel.ToSerilog(level);
            return created;
        }

        gate.Set(LogVueSerilogLevel.FromSerilog(levelSwitch.MinimumLevel));
        var bound = levelSwitch;
        gate.Changed += level => bound.MinimumLevel = LogVueSerilogLevel.ToSerilog(level);
        return bound;
    }
}