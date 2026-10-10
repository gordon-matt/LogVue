using LogVue;
using LogVue.Data.MySql;
using LogVue.Data.Npgsql;
using LogVue.Data.Sql;
using LogVue.Data.Sqlite;
using LogVue.Sample;
using Serilog;
using Serilog.Events;
using LoggingLevelSwitch = Serilog.Core.LoggingLevelSwitch;

var builder = WebApplication.CreateBuilder(args);

// One switch for the whole pipeline, so the level picker on the dashboard governs every sink.
var levelSwitch = new LoggingLevelSwitch(LogEventLevel.Debug);

builder.Services.AddLogVue(options =>
{
    builder.Configuration.GetSection(LogVueOptions.SectionName).Bind(options);
    options.LevelSwitch = levelSwitch;
});

string provider = builder.Configuration["Database:Provider"] ?? Constants.DatabaseProviders.Sqlite;
string? connectionString = builder.Configuration.GetConnectionString("LogVue");

switch (provider)
{
    case Constants.DatabaseProviders.Sqlite:
        builder.Services.AddLogVueSqlite(connectionString);
        break;

    case Constants.DatabaseProviders.SqlServer:
        builder.Services.AddLogVueSqlServer(connectionString!);
        break;

    case Constants.DatabaseProviders.Npgsql:
        builder.Services.AddLogVueNpgsql(connectionString!);
        break;

    case Constants.DatabaseProviders.MySql:
        builder.Services.AddLogVueMySql(connectionString!);
        break;

    default:
        throw new InvalidOperationException(
            $"Unknown Database:Provider '{provider}'. Expected Sqlite, SqlServer, Npgsql or MySql.");
}

builder.Services.AddSerilog((services, configuration) => configuration
    .MinimumLevel.ControlledBy(levelSwitch)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.LogVue(services));

builder.Services.AddHostedService<SampleTrafficGenerator>();

var app = builder.Build();

app.UseSerilogRequestLogging();

app.MapGet("/", () => Results.Redirect("/logvue"));

app.MapGet("/orders/{id:int}", (int id, ILogger<Program> logger) =>
{
    logger.LogInformation("Fetched order {OrderId} for {Customer}", id, "Contoso");
    return Results.Ok(new { id });
});

app.MapLogVue();

app.Run();
