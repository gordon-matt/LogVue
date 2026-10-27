using LogVue.Providers;
using LogVue.Services.Hosting;
using Microsoft.Extensions.Hosting;

namespace LogVue.Services.Logging;

/// <summary>
/// Drains <see cref="LogEventQueue"/> into the database in batches. Created with the host, which
/// is before migrations have run, so the first batch waits until the tables exist.
/// </summary>
internal sealed class LogIngestionService(
    LogEventQueue queue,
    IServiceScopeFactory scopeFactory,
    IOptions<LogVueOptions> options,
    LogVueDatabaseState databaseState,
    TimeProvider time) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            if (!await databaseState.Ready.WaitAsync(stoppingToken))
            {
                return;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                var batch = new List<LogEventData>(Math.Max(1, options.Value.BatchSizeLimit));

                if (!await TryFillBatchAsync(batch, stoppingToken))
                {
                    break;
                }

                await PersistAsync(batch, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    /// <summary>Writes whatever is already queued. Tests use this instead of waiting for the timer.</summary>
    internal async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        int batchSize = Math.Max(1, options.Value.BatchSizeLimit);
        var batch = new List<LogEventData>(batchSize);

        while (queue.Reader.TryRead(out var item))
        {
            batch.Add(item);

            if (batch.Count >= batchSize)
            {
                await PersistAsync(batch, cancellationToken);
                batch.Clear();
            }
        }

        if (batch.Count > 0)
        {
            await PersistAsync(batch, cancellationToken);
        }
    }

    private async Task<bool> TryFillBatchAsync(List<LogEventData> batch, CancellationToken stoppingToken)
    {
        if (!await queue.Reader.WaitToReadAsync(stoppingToken))
        {
            return false;
        }

        int batchSize = Math.Max(1, options.Value.BatchSizeLimit);
        DateTimeOffset deadline = time.GetUtcNow() + options.Value.BatchPeriod;

        while (batch.Count < batchSize)
        {
            while (batch.Count < batchSize && queue.Reader.TryRead(out var item))
            {
                batch.Add(item);
            }

            if (batch.Count >= batchSize)
            {
                break;
            }

            TimeSpan remaining = deadline - time.GetUtcNow();

            if (remaining <= TimeSpan.Zero)
            {
                break;
            }

            using var timeout = new CancellationTokenSource(remaining);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, timeout.Token);

            try
            {
                if (!await queue.Reader.WaitToReadAsync(linked.Token))
                {
                    return batch.Count > 0;
                }
            }
            catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        return batch.Count > 0;
    }

    private async Task PersistAsync(List<LogEventData> batch, CancellationToken cancellationToken)
    {
        var entries = new List<LogEntry>(batch.Count);

        foreach (var item in batch)
        {
            entries.Add(ToEntry(item));
        }

        var delay = TimeSpan.FromMilliseconds(200);

        while (true)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var repository = scope.ServiceProvider.GetRequiredService<IRepository<LogEntry>>();
                await repository.InsertAsync(entries);
                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception)
            {
                // The database can be briefly unavailable during startup or a failover. The queue
                // keeps accepting events, and this batch is retried rather than dropped.
                await Task.Delay(delay, cancellationToken);
                delay = TimeSpan.FromMilliseconds(Math.Min(delay.TotalMilliseconds * 2, 30_000));
            }
        }
    }

    private static LogEntry ToEntry(LogEventData logEvent)
    {
        var entry = new LogEntry
        {
            Timestamp = logEvent.Timestamp.UtcDateTime,
            Level = logEvent.Level,
            Message = logEvent.Message,
            MessageTemplate = logEvent.MessageTemplate,
            Exception = logEvent.Exception,
        };

        foreach (var property in logEvent.Properties)
        {
            string name = property.Name.Length <= Constants.MaxLengths.PropertyName
                ? property.Name
                : property.Name[..Constants.MaxLengths.PropertyName];

            entry.Properties.Add(new LogEntryProperty
            {
                Name = name,
                Value = property.Value,
            });
        }

        return entry;
    }
}
