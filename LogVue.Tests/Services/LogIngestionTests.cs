using LogVue.Data;
using LogVue.Services;
using LogVue.Services.Hosting;
using LogVue.Services.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LogVue.Tests.Services;

public class LogIngestionTests : IDisposable
{
    private readonly TestDatabase database = TestDatabase.CreateSqlite();

    public void Dispose() => database.Dispose();

    [Fact]
    public async Task Flush_StoresEvents_ButSkipsTheDashboardsOwnRequests()
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddLogVueServices()
            .AddSingleton<ILogVueDbContextFactory>(database.Factory)
            .BuildServiceProvider();

        services.GetRequiredService<LogVueDatabaseState>().MarkReady();
        var writer = services.GetRequiredService<ILogEventWriter>();

        writer.TryWrite(Event("HTTP {RequestPath}", ("RequestPath", "/logvue/_blazor")));
        writer.TryWrite(Event("HTTP {RequestPath}", ("RequestPath", "/logvuex")));
        writer.TryWrite(Event("Order placed"));

        await Ingestion(services).FlushAsync();

        using var connection = database.Entries.OpenConnection();
        var stored = connection.Query().Select(e => e.Message).ToList();

        Assert.Equal(["HTTP {RequestPath}", "Order placed"], stored.Order());
    }

    private static LogIngestionService Ingestion(ServiceProvider services) =>
        Assert.IsType<LogIngestionService>(services.GetServices<IHostedService>().Single(service => service is LogIngestionService));

    private static LogEventData Event(string template, params (string Name, string? Value)[] properties) => new()
    {
        Timestamp = DateTimeOffset.UtcNow,
        Level = LogVueLevel.Information,
        Message = template,
        MessageTemplate = template,
        Properties = properties.Select(property => new LogProperty(property.Name, property.Value)).ToArray(),
    };
}
