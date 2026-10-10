using LogVue;
using LogVue.Services.Logging;
using Serilog.Configuration;

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
        ///     .ReadFrom.Configuration(builder.Configuration)
        ///     .WriteTo.Console()
        ///     .WriteTo.LogVue(services));
        /// </code>
        /// </summary>
        public LoggerConfiguration LogVue(IServiceProvider services)
        {
            var options = services.GetRequiredService<IOptions<LogVueOptions>>().Value;

            return writeTo.Sink(
                services.GetRequiredService<LogVueSink>(),
                new BatchingOptions
                {
                    BatchSizeLimit = options.BatchSizeLimit,
                    BufferingTimeLimit = options.BatchPeriod,
                    QueueLimit = options.QueueLimit,
                },
                levelSwitch: options.LevelSwitch);
        }
    }
}
