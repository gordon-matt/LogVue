using System.Diagnostics;
using LogVue.Providers.Serilog;
using Serilog.Events;
using Serilog.Parsing;

namespace LogVue.Tests.Services;

public class LogVueSerilogSinkTests
{
    [Fact]
    public void ToEvent_RendersStringsWithoutQuotes_AndKeepsRawPropertyValues()
    {
        var logEvent = CreateEvent(
            "Order {OrderId} for {Customer} took {Elapsed:0.0} ms",
            ("OrderId", new ScalarValue(42)),
            ("Customer", new ScalarValue("Contoso")),
            ("Elapsed", new ScalarValue(12.345)));

        var entry = LogVueSerilogSink.ToEvent(logEvent);

        Assert.Equal("Order 42 for Contoso took 12.3 ms", entry.Message);
        Assert.Equal("Order {OrderId} for {Customer} took {Elapsed:0.0} ms", entry.MessageTemplate);
        Assert.Equal("12.345", entry.Properties.Single(p => p.Name == "Elapsed").Value);
        Assert.Equal("Contoso", entry.Properties.Single(p => p.Name == "Customer").Value);
        Assert.Equal(DateTimeKind.Utc, entry.Timestamp.UtcDateTime.Kind);
    }

    [Fact]
    public void ToEvent_AddsTraceAndSpanIds()
    {
        var traceId = ActivityTraceId.CreateRandom();
        var spanId = ActivitySpanId.CreateRandom();
        var logEvent = new LogEvent(
            DateTimeOffset.UtcNow, LogEventLevel.Information, null,
            new MessageTemplateParser().Parse("Hello"), [], traceId, spanId);

        var entry = LogVueSerilogSink.ToEvent(logEvent);

        Assert.Equal(traceId.ToHexString(), entry.Properties.Single(p => p.Name == "TraceId").Value);
        Assert.Equal(spanId.ToHexString(), entry.Properties.Single(p => p.Name == "SpanId").Value);
    }

    [Fact]
    public void Emit_QueuesTheMappedEvent()
    {
        var writer = new ListWriter();
        var sink = new LogVueSerilogSink(writer);

        sink.Emit(CreateEvent("Order placed"));

        Assert.Equal(["Order placed"], writer.Events.Select(e => e.Message));
    }

    private static LogEvent CreateEvent(string template, params (string Name, LogEventPropertyValue Value)[] properties) =>
        new(
            DateTimeOffset.UtcNow,
            LogEventLevel.Information,
            null,
            new MessageTemplateParser().Parse(template),
            properties.Select(p => new LogEventProperty(p.Name, p.Value)));

    private sealed class ListWriter : ILogEventWriter
    {
        public List<LogEventData> Events { get; } = [];

        public bool TryWrite(LogEventData logEvent)
        {
            Events.Add(logEvent);
            return true;
        }
    }
}