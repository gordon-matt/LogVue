using Microsoft.Extensions.Hosting;

namespace LogVue.Services.Hosting;

/// <summary>
/// Deletes entries older than <see cref="LogVueOptions.RetentionDays"/>. Runs regardless of
/// <see cref="LogVueOptions.ReadOnly"/>: retention is the operator's policy, not a dashboard action.
/// </summary>
public sealed class LogVueRetentionService(
    ILogger<LogVueRetentionService> logger,
    IServiceScopeFactory scopeFactory,
    IOptions<LogVueOptions> options,
    LogVueDatabaseState databaseState,
    TimeProvider timeProvider) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;

        if (settings.RetentionDays <= 0 || !await databaseState.Ready.WaitAsync(stoppingToken))
        {
            return;
        }

        using var timer = new PeriodicTimer(settings.RetentionInterval, timeProvider);

        do
        {
            await PurgeAsync(settings, stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task PurgeAsync(LogVueOptions settings, CancellationToken cancellationToken)
    {
        var cutoff = timeProvider.GetUtcNow().UtcDateTime.AddDays(-settings.RetentionDays);

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<LogEntry>>();

            int deleted = await repository.DeleteAsync(e => e.Timestamp < cutoff, new ContextOptions
            {
                CommandTimeout = settings.DeleteCommandTimeoutSeconds,
                CancellationToken = cancellationToken,
            });

            if (deleted > 0)
            {
                logger.LogInformation(
                    "LogVue retention removed {Count} entries older than {Cutoff:u}",
                    deleted,
                    cutoff);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // The next tick tries again; one failed purge shouldn't end retention for the process.
            logger.LogError(ex, "LogVue retention could not remove entries older than {Cutoff:u}", cutoff);
        }
    }
}
