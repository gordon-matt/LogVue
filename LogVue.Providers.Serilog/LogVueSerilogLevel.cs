using Serilog.Events;

namespace LogVue.Providers.Serilog;

internal static class LogVueSerilogLevel
{
    public static LogVueLevel FromSerilog(LogEventLevel level) => (LogVueLevel)(int)level;

    public static LogEventLevel ToSerilog(LogVueLevel level) => (LogEventLevel)(int)level;
}