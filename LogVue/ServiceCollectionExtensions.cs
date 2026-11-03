using Heroicsoft.NetForge.Abstractions.Auth;
using Heroicsoft.NetForge.Components;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LogVue;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the LogVue dashboard, its services and the <c>ILogger</c> provider. Pair with a
        /// storage provider (<c>AddLogVueSqlite</c>, <c>AddLogVueSqlServer</c>, <c>AddLogVueNpgsql</c>
        /// or <c>AddLogVueMySql</c>) and <c>app.MapLogVue()</c>. Hosts that log only through Serilog's
        /// pipeline also call <c>WriteTo.LogVue</c> from the <c>LogVue.Providers.Serilog</c> package.
        /// </summary>
        public IServiceCollection AddLogVue(Action<LogVueOptions>? configure = null)
        {
            services.AddLogVueServices(configure);

            // Safe alongside a host that is itself a Blazor app: both calls are idempotent.
            services.AddRazorComponents().AddInteractiveServerComponents();
            // Before AddNetForgeComponents, which otherwise supplies "no sign-in". A host that already
            // chose a mode keeps it. The shell reads ForgeUiOptions for the shared theme key, and a host
            // that also calls AddNetForgeComponents keeps its own key.
            services.TryAddSingleton<IAuthProviderService>(new AuthProviderService(AuthProvider.Identity, null));
            services.AddNetForgeComponents();
            services.AddNetForgeMenuContributor<LogVueMenuContributor>();

            return services;
        }
    }
}