using Heroicsoft.NetForge.Abstractions.Auth;
using Heroicsoft.NetForge.Components;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LogVue;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the LogVue dashboard and the services behind it. Pair with a storage
        /// provider (<c>AddLogVueSqlite</c>, <c>AddLogVueSqlServer</c>, <c>AddLogVueNpgsql</c> or
        /// <c>AddLogVueMySql</c>), <c>.WriteTo.LogVue(services)</c> in the Serilog configuration,
        /// and <c>app.MapLogVue()</c>.
        /// </summary>
        public IServiceCollection AddLogVue(Action<LogVueOptions>? configure = null)
        {
            services.AddLogVueServices(configure);

            // Safe alongside a host that is itself a Blazor app: both calls are idempotent.
            services.AddRazorComponents().AddInteractiveServerComponents();
            // The shell reads ForgeUiOptions for the shared theme key. A host that also calls
            // AddNetForgeComponents keeps its own key; this only fills the options in when it does not.
            services.AddNetForgeComponents();
            services.TryAddSingleton<IAuthProviderService>(new AuthProviderService(AuthProvider.Identity, null));

            return services;
        }
    }
}