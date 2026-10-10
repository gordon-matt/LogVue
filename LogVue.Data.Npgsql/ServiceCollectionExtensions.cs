using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LogVue.Data.Npgsql;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Stores LogVue's logs in PostgreSQL.</summary>
        public IServiceCollection AddLogVueNpgsql(string connectionString)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

            services.RemoveAll<ILogVueDbContextFactory>();
            services.AddSingleton<ILogVueDbContextFactory>(new LogVueDbContextFactory(connectionString));

            services.RemoveAll<ILogVueQueryDialect>();
            services.AddSingleton<ILogVueQueryDialect, NpgsqlQueryDialect>();

            return services;
        }
    }
}