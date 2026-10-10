using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LogVue.Data.Sqlite;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Stores LogVue's logs in SQLite.
        /// </summary>
        /// <param name="connectionString">
        /// A SQLite connection string. When omitted, logs go to <c>LogVue.db</c> under the user's
        /// local application data folder, which survives redeployments of the app's own folder.
        /// </param>
        public IServiceCollection AddLogVueSqlite(string? connectionString = null)
        {
            string resolved = string.IsNullOrWhiteSpace(connectionString)
                ? $"Data Source={DefaultDatabasePath()}"
                : connectionString;

            services.RemoveAll<ILogVueDbContextFactory>();
            services.AddSingleton<ILogVueDbContextFactory>(new LogVueDbContextFactory(resolved));

            services.RemoveAll<ILogVueQueryDialect>();
            services.AddSingleton<ILogVueQueryDialect, DefaultQueryDialect>();

            return services;
        }
    }

    private static string DefaultDatabasePath()
    {
        string appName = Assembly.GetEntryAssembly()?.GetName().Name ?? "LogVue";

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            appName,
            "LogVue.db");
    }
}