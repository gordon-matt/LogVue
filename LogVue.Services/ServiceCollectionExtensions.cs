using LogVue.Services.Hosting;
using LogVue.Services.LogEntries;
using LogVue.Services.Logging;
using LogVue.Services.SavedQueries;
using LogVue.Services.Settings;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LogVue.Services;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers LogVue's services, sink and background work. A storage provider must be added
        /// as well, from one of the <c>LogVue.Data.*</c> packages.
        /// </summary>
        public IServiceCollection AddLogVueServices(Action<LogVueOptions>? configure = null)
        {
            services.AddOptions<LogVueOptions>()
                .Configure(options => configure?.Invoke(options))
                .PostConfigure(options => options.BasePath = NormalizeBasePath(options.BasePath));

            services.TryAddSingleton(TimeProvider.System);
            services.TryAddSingleton<ILogVueQueryDialect, DefaultQueryDialect>();

            services.TryAddSingleton<LogVueDatabaseState>();
            services.TryAddSingleton<LogVueSink>();

            // Closed registrations against LogVue's own factory. A host built on Extenso maps the
            // open IRepository<> to its own IDbContextFactory, and closed registrations win, so
            // the two never cross.
            services.TryAddScoped(LogVueRepository<LogEntry>);
            services.TryAddScoped(LogVueRepository<LogEntryProperty>);
            services.TryAddScoped(LogVueRepository<SavedQuery>);

            services.TryAddScoped<ILogEntryService, LogEntryService>();
            services.TryAddScoped<ISavedQueryService, SavedQueryService>();
            services.TryAddSingleton<IMinimumLevelService, MinimumLevelService>();

            services.AddHostedService<LogVueDatabaseInitializer>();
            services.AddHostedService<LogVueRetentionService>();

            return services;
        }
    }

    private static IRepository<TEntity> LogVueRepository<TEntity>(IServiceProvider provider)
        where TEntity : class, IEntity =>
        new EntityFrameworkRepository<TEntity>(provider.GetRequiredService<ILogVueDbContextFactory>());

    private static string NormalizeBasePath(string? basePath)
    {
        string trimmed = (basePath ?? string.Empty).Trim().Trim('/');
        return trimmed.Length == 0 ? "/logvue" : "/" + trimmed;
    }
}