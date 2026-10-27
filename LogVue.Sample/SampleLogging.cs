using NLog.Extensions.Logging;
using Serilog;
using Serilog.Events;
using LoggingLevelSwitch = Serilog.Core.LoggingLevelSwitch;

namespace LogVue.Sample;

/// <summary>
/// Picks the logging framework for this demo. NLog and log4net are added as Microsoft logging
/// providers, so LogVue's own provider still receives every <c>ILogger</c> call. Serilog replaces
/// that pipeline, so it is wired with <c>WriteTo.LogVue</c> instead.
/// </summary>
internal static class SampleLogging
{
    public const string Microsoft = "Microsoft";

    public const string NLog = "NLog";

    public const string Log4Net = "Log4Net";

    public const string Serilog = "Serilog";

    /// <summary>
    /// Call before <c>AddLogVue</c>. <c>ClearProviders</c> removes every <c>ILoggerProvider</c>
    /// already registered, including LogVue's if it was added first.
    /// </summary>
    public static string Add(WebApplicationBuilder builder)
    {
        string provider = Normalize(builder.Configuration["Sample:LoggingProvider"]);

        switch (provider)
        {
            case NLog:
                builder.Logging.ClearProviders();
                builder.Logging.AddNLog(builder.Configuration);
                break;

            case Log4Net:
                builder.Logging.ClearProviders();
                builder.Logging.AddLog4Net("log4net.config");
                break;
        }

        return provider;
    }

    public static void AddSerilogSink(WebApplicationBuilder builder, string provider)
    {
        if (provider != Serilog)
        {
            return;
        }

        var levelSwitch = new LoggingLevelSwitch(LogEventLevel.Debug);
        builder.Services.AddLogVueSerilog(levelSwitch);
        builder.Services.AddSerilog((services, configuration) => configuration
            .MinimumLevel.ControlledBy(levelSwitch)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.LogVue(services, levelSwitch));
    }

    private static string Normalize(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        null or "" or "microsoft" => Microsoft,
        "nlog" => NLog,
        "log4net" => Log4Net,
        "serilog" => Serilog,
        _ => throw new InvalidOperationException(
            $"Unknown Sample:LoggingProvider '{value}'. Expected {Microsoft}, {NLog}, {Log4Net} or {Serilog}."),
    };
}