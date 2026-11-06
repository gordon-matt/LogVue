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
    /// <summary>
    /// Starts the value.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Checked here rather than at registration so the provider call can come before or after
        // AddLogVue. A host that has no connection yet, such as one showing its installation page,
        // can start without a provider. Nothing is stored until one is registered and the host restarts.
        var contextFactory = services.GetService<ILogVueDbContextFactory>();
        if (contextFactory is null)
        {
            databaseState.MarkFailed();
            logger.LogWarning(
                "LogVue has no storage provider, so log entries will not be stored. Call AddLogVueSqlite, AddLogVueSqlServer, AddLogVueNpgsql or AddLogVueMySql from the matching LogVue.Data package.");
            return;
        }

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

    /// <summary>
    /// Stops the value.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}