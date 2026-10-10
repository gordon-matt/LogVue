using System.Diagnostics;
using LogVue.Data;
using LogVue.Services;
using LogVue.Services.Hosting;
using LogVue.Services.Logging;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Events;
using Serilog.Parsing;

namespace LogVue.Tests.Services;

public class LogVueSinkTests : IDisposable
{
    private readonly TestDatabase database = TestDatabase.CreateSqlite();

    public void Dispose() => database.Dispose();

    [Fact]
    public void ToEntry_RendersStringsWithoutQuotes_AndKeepsRawPropertyValues()
    {
        var logEvent = CreateEvent(
            "Order {OrderId} for {Customer} took {Elapsed:0.0} ms",
            ("OrderId", new ScalarValue(42)),
            ("Customer", new ScalarValue("Contoso")),
            ("Elapsed", new ScalarValue(12.345)));

        var entry = LogVueSink.ToEntry(logEvent);

        Assert.Equal("Order 42 for Contoso took 12.3 ms", entry.Message);
        Assert.Equal("Order {OrderId} for {Customer} took {Elapsed:0.0} ms", entry.MessageTemplate);
        Assert.Equal("12.345", entry.Properties.Single(p => p.Name == "Elapsed").Value);
        Assert.Equal("Contoso", entry.Properties.Single(p => p.Name == "Customer").Value);
        Assert.Equal(DateTimeKind.Utc, entry.Timestamp.Kind);
    }

    [Fact]
    public void ToEntry_AddsTraceAndSpanIds()
    {
        var traceId = ActivityTraceId.CreateRandom();
        var spanId = ActivitySpanId.CreateRandom();
        var logEvent = new LogEvent(
            DateTimeOffset.UtcNow, LogEventLevel.Information, null,
            new MessageTemplateParser().Parse("Hello"), [], traceId, spanId);

        var entry = LogVueSink.ToEntry(logEvent);

        Assert.Equal(traceId.ToHexString(), entry.Properties.Single(p => p.Name == "TraceId").Value);
        Assert.Equal(spanId.ToHexString(), entry.Properties.Single(p => p.Name == "SpanId").Value);
    }

    [Fact]
    public async Task EmitBatchAsync_StoresEvents_ButSkipsTheDashboardsOwnRequests()
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddLogVueServices()
            .AddSingleton<ILogVueDbContextFactory>(database.Factory)
            .BuildServiceProvider();

        services.GetRequiredService<LogVueDatabaseState>().MarkReady();
        var sink = services.GetRequiredService<LogVueSink>();

        await sink.EmitBatchAsync(
        [
            CreateEvent("HTTP {RequestPath}", ("RequestPath", new ScalarValue("/logvue/_blazor"))),
            CreateEvent("HTTP {RequestPath}", ("RequestPath", new ScalarValue("/logvuex"))),
            CreateEvent("Order placed"),
        ]);

        using var connection = database.Entries.OpenConnection();
        var stored = connection.Query().Select(e => e.Message).ToList();

        Assert.Equal(["HTTP /logvuex", "Order placed"], stored.Order());
    }

    private static LogEvent CreateEvent(string template, params (string Name, LogEventPropertyValue Value)[] properties) =>
        new(
            DateTimeOffset.UtcNow,
            LogEventLevel.Information,
            null,
            new MessageTemplateParser().Parse(template),
            properties.Select(p => new LogEventProperty(p.Name, p.Value)));
}