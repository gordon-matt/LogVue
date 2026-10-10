using Microsoft.Extensions.Hosting;

namespace LogVue.Services.Hosting;

/// <summary>
/// Applies LogVue's migrations before the host starts serving requests.
/// </summary>
public sealed class LogVueDatabaseInitializer(
    ILogger<LogVueDatabaseInitializer> logger,
    IServiceProvider services,
    IOptions<LogVueOptions> options,
    LogVueDatabaseState databaseState) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Checked here rather than at registration so the provider call can come before or after
        // AddLogVue. A missing provider is a configuration mistake, so it stops the host.
        var contextFactory = services.GetService<ILogVueDbContextFactory>()
            ?? throw new InvalidOperationException(
                "LogVue has no storage provider. Call AddLogVueSqlite, AddLogVueSqlServer, " +
                "AddLogVueNpgsql or AddLogVueMySql from the matching LogVue.Data package.");

        if (!options.Value.AutoMigrate)
        {
            databaseState.MarkReady();
            return;
        }

        try
        {
            await using var context = contextFactory.GetContext();
            await context.Database.MigrateAsync(cancellationToken);

            databaseState.MarkReady();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Unlike the provider check, a database that is unreachable right now is not a reason
            // to stop the application LogVue is observing.
            databaseState.MarkFailed();
            logger.LogError(ex, "LogVue could not prepare its {Provider} database; log entries will not be stored", contextFactory.ProviderName);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
