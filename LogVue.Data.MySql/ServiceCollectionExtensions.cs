using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LogVue.Data.MySql;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Stores LogVue's logs in MySQL.</summary>
        public IServiceCollection AddLogVueMySql(string connectionString)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

            services.RemoveAll<ILogVueDbContextFactory>();
            services.AddSingleton<ILogVueDbContextFactory>(new LogVueDbContextFactory(connectionString));

            services.RemoveAll<ILogVueQueryDialect>();
            services.AddSingleton<ILogVueQueryDialect, MySqlQueryDialect>();

            return services;
        }
    }
}
