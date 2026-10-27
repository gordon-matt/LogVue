using System.Diagnostics;
using LogVue.Data;
using LogVue.Data.Entities;
using LogVue.Services;
using LogVue.Services.Hosting;
using LogVue.Services.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LogVue.Tests.Services;

public class LogVueLoggerTests : IDisposable
{
    private readonly TestDatabase database = TestDatabase.CreateSqlite();

    public void Dispose() => database.Dispose();

    [Fact]
    public async Task Log_StoresTemplatePropertiesCategoryAndTrace_AndSkipsDebugBelowTheMinimum()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
        };
        ActivitySource.AddActivityListener(listener);
        using var source = new ActivitySource(nameof(LogVueLoggerTests));

        await using var services = CreateServices(options => options.MinimumLevel = LogVueLevel.Information);
        services.GetRequiredService<LogVueDatabaseState>().MarkReady();

        var logger = services.GetRequiredService<ILogger<LogVueLoggerTests>>();
        using var activity = source.StartActivity("fetch");
        Assert.NotNull(activity);
        using var scope = logger.BeginScope(new Dictionary<string, object> { ["Tenant"] = "Contoso" });

        logger.LogDebug("Ignored {OrderId}", 1);
        logger.LogInformation("Order {OrderId} placed", 42);

        await Ingestion(services).FlushAsync();

        using var context = database.Factory.GetContext();
        var stored = context.Set<LogEntry>().Include(entry => entry.Properties).ToList();

        var entry = Assert.Single(stored);
        Assert.Equal("Order 42 placed", entry.Message);
        Assert.Equal("Order {OrderId} placed", entry.MessageTemplate);
        Assert.Equal(LogVueLevel.Information, entry.Level);
        Assert.Equal("42", entry.Properties.Single(property => property.Name == "OrderId").Value);
        Assert.Equal("Contoso", entry.Properties.Single(property => property.Name == "Tenant").Value);
        Assert.Equal(typeof(LogVueLoggerTests).FullName, entry.Properties.Single(property => property.Name == "SourceContext").Value);
        Assert.Equal(activity.TraceId.ToHexString(), entry.Properties.Single(property => property.Name == "TraceId").Value);
        Assert.DoesNotContain(entry.Properties, property => property.Name == "{OriginalFormat}");
    }

    [Fact]
    public async Task Log_SkipsRequestsUnderTheDashboardPath()
    {
        await using var services = CreateServices();
        services.GetRequiredService<LogVueDatabaseState>().MarkReady();
        var logger = services.GetRequiredService<ILogger<LogVueLoggerTests>>();

        logger.LogInformation("HTTP {RequestPath}", "/logvue/logs");
        logger.LogInformation("HTTP {RequestPath}", "/orders");

        await Ingestion(services).FlushAsync();

        using var connection = database.Entries.OpenConnection();
        Assert.Equal(["HTTP /orders"], connection.Query().Select(entry => entry.Message).ToList());
    }

    private ServiceProvider CreateServices(Action<LogVueOptions>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLogVueServices(configure);
        services.AddSingleton<ILogVueDbContextFactory>(database.Factory);
        return services.BuildServiceProvider();
    }

    private static LogIngestionService Ingestion(ServiceProvider services) =>
        Assert.IsType<LogIngestionService>(services.GetServices<IHostedService>().Single(service => service is LogIngestionService));
}
