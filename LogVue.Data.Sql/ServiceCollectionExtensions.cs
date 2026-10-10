using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LogVue.Data.Sql;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Stores LogVue's logs in SQL Server.</summary>
        public IServiceCollection AddLogVueSqlServer(string connectionString)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

            services.RemoveAll<ILogVueDbContextFactory>();
            services.AddSingleton<ILogVueDbContextFactory>(new LogVueDbContextFactory(connectionString));

            services.RemoveAll<ILogVueQueryDialect>();
            services.AddSingleton<ILogVueQueryDialect, DefaultQueryDialect>();

            return services;
        }
    }
}
