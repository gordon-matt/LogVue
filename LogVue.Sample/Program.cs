using LogVue;
using LogVue.Data.MySql;
using LogVue.Data.Npgsql;
using LogVue.Data.Sql;
using LogVue.Data.Sqlite;
using LogVue.Providers;
using LogVue.Sample;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLogVue(options =>
{
    builder.Configuration.GetSection(LogVueOptions.SectionName).Bind(options);
    options.MinimumLevel = LogVueLevel.Debug;
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

builder.Services.AddHostedService<SampleTrafficGenerator>();

var app = builder.Build();

app.UseStaticFiles();

app.MapGet("/", () => Results.Redirect("/logvue"));

app.MapGet("/orders/{id:int}", (int id, ILogger<Program> logger) =>
{
    logger.LogInformation("Fetched order {OrderId} for {Customer}", id, "Contoso");
    return Results.Ok(new { id });
});

app.MapLogVue();

app.Run();
