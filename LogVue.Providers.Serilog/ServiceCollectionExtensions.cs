using LogVue.Providers;
using LogVue.Providers.Serilog;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog.Core;

namespace LogVue;

public static class SerilogServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Points the dashboard's level picker at a Serilog <see cref="LoggingLevelSwitch"/>.
        /// Pass the same instance to <c>MinimumLevel.ControlledBy</c> and <c>WriteTo.LogVue</c>
        /// when the picker should govern every Serilog sink, not only what LogVue stores.
        /// Call this as well as <c>AddLogVue</c>; either order works.
        /// </summary>
        public IServiceCollection AddLogVueSerilog(LoggingLevelSwitch levelSwitch)
        {
            services.Replace(ServiceDescriptor.Singleton<ILogLevelGate>(new SerilogLogLevelGate(levelSwitch)));
            return services;
        }
    }
}
