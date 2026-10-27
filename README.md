<img src="https://github.com/gordon-matt/LogVue/blob/master/_Misc/Logo.png" alt="Logo" width="300" />

An in-process log dashboard for ASP.NET Core. LogVue is a `Microsoft.Extensions.Logging` provider
plus a Blazor dashboard shipped as a Razor Class Library: add a few lines to `Program.cs` and your
app stores its own logs and serves a page to search, tail and clean them up. No separate log
server. Anything written with `ILogger` is stored, including Serilog, NLog and log4net when they
are registered as Microsoft logging providers. Apps that replace that pipeline and log only
through Serilog use the optional `LogVue.Providers.Serilog` package.

LogVue is a modernised take on [Sejil](https://github.com/alaatm/Sejil), with:

- **A Blazor UI**: an overview with level totals, volume and the noisiest sources, plus a virtualised log search with live tail, level counts, a details panel with click-to-filter
  properties, saved queries, light and dark themes, local or UTC times, and keyboard navigation
  (`/` to search, `j`/`k` to move, `Esc` to close).
- **Deletes**: a single entry, everything matching the current view, everything older than N
  days, or everything. Optional automatic retention.
- **Four databases**: SQLite, SQL Server, PostgreSQL and MySQL.
- **Performance work**: batched writes, keyset paging instead of `OFFSET`, live tail that only
  fetches new rows, set-based deletes (`ExecuteDelete`), and the search box compiles to SQL
  instead of being filtered in memory.

## Screenshots

<img src="https://github.com/gordon-matt/LogVue/blob/master/_Misc/Screenshots/Theme_Dark.png" alt="Dark Theme" />

<img src="https://github.com/gordon-matt/LogVue/blob/master/_Misc/Screenshots/Theme_Light.png" alt="Light Theme" />

## Getting started

Reference `LogVue` and one provider package:

| Database   | Package              | Registration                            |
|------------|----------------------|-----------------------------------------|
| SQLite     | `LogVue.Data.Sqlite` | `services.AddLogVueSqlite(connString?)` |
| SQL Server | `LogVue.Data.Sql`    | `services.AddLogVueSqlServer(connString)` |
| PostgreSQL | `LogVue.Data.Npgsql` | `services.AddLogVueNpgsql(connString)`  |
| MySQL      | `LogVue.Data.MySql`  | `services.AddLogVueMySql(connString)`   |

Then, in `Program.cs`:

```csharp
using LogVue;
using LogVue.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLogVue(options => options.RetentionDays = 30);
builder.Services.AddLogVueSqlite("Data Source=logs.db");

var app = builder.Build();

app.MapLogVue().RequireAuthorization();

app.Run();
```

Browse to `/logvue` for the overview, or `/logvue/logs` to search. LogVue applies its migrations at startup, so the tables appear on first run.
Its tables are prefixed `LogVue_`, and it keeps its own migrations history table, so it can share
a database with your app.

If you call `AddLogVueSqlite()` with no connection string, logs go to
`%LOCALAPPDATA%\{YourApp}\LogVue.db` (or `~/.local/share/{YourApp}/LogVue.db`), outside the
deployment folder.

### Protect the dashboard

`MapLogVue()` returns the route group, so secure it like any other endpoint:

```csharp
app.MapLogVue().RequireAuthorization("Admins");
```

Set `ReadOnly = true` to let people browse without deleting entries or changing the level.

### Serilog's own pipeline

`AddSerilog()` replaces the Microsoft logger and does not forward events to other providers, so the
provider above never sees them. Reference `LogVue.Providers.Serilog` and write Serilog to LogVue
directly:

```csharp
using LogVue;
using LogVue.Data.Sqlite;
using Serilog;
using Serilog.Core;
using Serilog.Events;

var levelSwitch = new LoggingLevelSwitch(LogEventLevel.Information);

builder.Services.AddLogVue(options => options.RetentionDays = 30);
builder.Services.AddLogVueSqlite("Data Source=logs.db");
builder.Services.AddLogVueSerilog(levelSwitch);

builder.Services.AddSerilog((services, configuration) => configuration
    .MinimumLevel.ControlledBy(levelSwitch)
    .WriteTo.LogVue(services, levelSwitch));
```

`AddLogVueSerilog` points the dashboard's level picker at that switch, so one control governs every
Serilog sink. Call `WriteTo.LogVue(services)` without a switch when only the level LogVue stores
should change.

Leave `writeToProviders` off. Turning it on as well as `WriteTo.LogVue` stores each event twice.
Set `UseLoggerProvider` to false if a host has to forward events to other Microsoft providers and
still use the sink.

### Options

Bind them from configuration with
`builder.Configuration.GetSection(LogVueOptions.SectionName).Bind(options)`, or set them in code.

| Option                        | Default    | Description |
|-------------------------------|------------|-------------|
| `Title`                       | `LogVue`   | Browser tab title. Shown in the header when it is not `LogVue` and `LogoPath` is unset. |
| `LogoPath`                    |            | Header image. Overrides `Title`. Root-relative (`/logo.png`), app-relative (`~/logo.png`), or an absolute URL. |
| `BasePath`                    | `/logvue`  | Where the dashboard is mounted. |
| `Area`                        |            | Theme area. Set `Admin` to share the back-office theme; leave unset to follow the public site. |
| `ReadOnly`                    | `false`    | Hides and refuses deletes and level changes. |
| `RetentionDays`               | `0`        | Deletes entries older than this. `0` keeps everything. |
| `RetentionInterval`           | 1 hour     | How often retention runs. |
| `AutoMigrate`                 | `true`     | Applies migrations at startup. Turn off if schema changes go through your pipeline. |
| `MinimumLevel`                | Information | Lowest level stored at startup. The picker changes it afterwards. |
| `UseLoggerProvider`           | `true`     | Stores `ILogger` calls. Turn off when the Serilog sink is already capturing them. |
| `IgnoreDashboardRequests`     | `true`     | Skips events raised while serving the dashboard itself. |
| `PageSize`                    | `100`      | Entries fetched per scroll step. |
| `LiveRefreshInterval`         | 3 seconds  | Polling interval while live tail is on. |
| `BatchSizeLimit`              | `500`      | Most events written per database round trip. |
| `BatchPeriod`                 | 2 seconds  | How long events wait in memory before being written. |
| `QueueLimit`                  | `100000`   | Events buffered while the database is unavailable; the oldest are dropped beyond this. |
| `DeleteCommandTimeoutSeconds` | `300`      | Command timeout for deletes on large tables. |

## Searching

Plain words are matched anywhere in the message, exception or property values, ignoring case.
Every word must match. Wrap a phrase in double quotes to match it exactly.

A query containing `=`, `!=`, `like` or parentheses is a filter expression:

```text
StatusCode = 500
SourceContext like '%Orders%'
level = warning or level = error
RequestPath like '/api/%' and StatusCode != 200
exception like '%SqlException%'
message not like '%health%'
```

- Compare a property with `=`, `!=`, `like` or `not like`. Combine comparisons with `and`, `or`
  and parentheses.
- Besides property names you can use `message`, `messageTemplate`, `exception` and `level`.
- In `like`, `%` matches anything and `_` matches one character. All matching ignores case.
- `Property != 'x'` also matches entries that don't have the property.

The time range, level and exception filters combine with the query. They all live in the URL, so
you can bookmark or share a view.

## Hosting notes

The dashboard is a Blazor Server app, so it needs the framework's `blazor.web.js` from the host's
static web assets. The ASP.NET Core SDK only includes that file when the host has `.razor` files
of its own. The LogVue package fixes this for any host by setting `RequiresAspNetWebAssets` in a
props file that NuGet imports automatically.

- **Project reference instead of a package?** Add the property to your host's `.csproj` yourself:

  ```xml
  <PropertyGroup>
    <RequiresAspNetWebAssets>true</RequiresAspNetWebAssets>
  </PropertyGroup>
  ```

- **Running from build output outside Development?** Static web assets only come from the NuGet
  cache in Development. In other environments, publish the app (the file lands in
  `wwwroot/_framework`) or call `builder.WebHost.UseStaticWebAssets()`. If the file is missing,
  `/logvue/_framework/blazor.web.js` returns a 404 explaining this.

The dashboard uses the NetForge shell and the host's `ForgeUiOptions.ThemeStorageKey`, so a theme
change here follows the rest of the site. Those stylesheets are static web assets, so the host
calls `UseStaticFiles` (the sample does). LogVue's own page CSS and script stay embedded and are
served from the mount path.

## Repository layout

| Project                 | Contents |
|-------------------------|----------|
| `LogVue`                | The Razor Class Library: `AddLogVue`, `MapLogVue` and the Blazor dashboard. |
| `LogVue.Providers.Abstractions` | The level, the event, and the writer and level-gate contracts a provider builds on. |
| `LogVue.Providers.Serilog` | The Serilog sink. Referenced only by hosts that log through Serilog's own pipeline. |
| `LogVue.Services`       | The `ILogger` provider, ingestion, the query language, and services returning `Ardalis.Result`. |
| `LogVue.Data`           | Entities, the base `DbContext` and the provider abstractions. |
| `LogVue.Data.*`         | One project per database, with its context factory and migrations. |
| `LogVue.Sample`         | A demo app that generates traffic. `dotnet run --project LogVue.Sample`, then open http://localhost:5180/logvue. |
| `LogVue.Tests`          | xUnit tests. |

The sample writes its traffic with `ILogger`. Set `Sample:LoggingProvider` in `LogVue.Sample/appsettings.json`
to `Microsoft`, `NLog`, `Log4Net` or `Serilog`. NLog and log4net are registered as Microsoft logging
providers, so LogVue stores the same events. Serilog replaces that pipeline, and the sample sends it
to LogVue with `WriteTo.LogVue`.

Data access goes through [Extenso](https://github.com/gordon-matt/Extenso)'s
`EntityFrameworkRepository<T>`.

### Running the tests

`dotnet test` runs the unit tests and the SQLite provider test. The SQL Server, PostgreSQL and
MySQL provider tests run when their environment variable holds a server connection string. Each
test creates and drops its own uniquely named database.

```powershell
$env:LOGVUE_TEST_SQLSERVER = 'Server=(localdb)\MSSQLLocalDB;Trusted_Connection=True;TrustServerCertificate=True'

docker run -d --name logvue-postgres -e POSTGRES_PASSWORD=LogVue_Test1 -p 55432:5432 postgres:17
$env:LOGVUE_TEST_NPGSQL = 'Host=localhost;Port=55432;Username=postgres;Password=LogVue_Test1'

docker run -d --name logvue-mysql -e MYSQL_ROOT_PASSWORD=LogVue_Test1 -p 53306:3306 mysql:8.4
$env:LOGVUE_TEST_MYSQL = 'Server=localhost;Port=53306;User ID=root;Password=LogVue_Test1'

dotnet test
```

### Adding a migration

Each provider project has a design-time factory, so run `dotnet ef` against each one:

```powershell
dotnet ef migrations add <Name> --project LogVue.Data.Sqlite
dotnet ef migrations add <Name> --project LogVue.Data.Sql
dotnet ef migrations add <Name> --project LogVue.Data.Npgsql
dotnet ef migrations add <Name> --project LogVue.Data.MySql
```

## License and attribution

LogVue is derived from [Sejil](https://github.com/alaatm/Sejil) by Alaa Masoud, licensed under
the Apache License 2.0.

## Support

If you find this project useful, consider supporting its development.

<p align="center">
  <a href="https://buymeacoffee.com/vnmatt">
    <img src="https://github.com/gordon-matt/MyResume/blob/gh-pages/assets/images/BuyMeACoffee_Logo.png"
         alt="Buy Me a Coffee"
         height="40">
  </a>&nbsp;&nbsp;&nbsp;
  <a href="https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=gordon_matt%40live%2ecom&lc=AU&currency_code=AUD&bn=PP%2dDonationsBF%3abtn_donateCC_LG%2egif%3aNonHosted">
    <img src="https://github.com/gordon-matt/MyResume/blob/gh-pages/assets/images/PayPal_Logo.png"
         alt="Donate with PayPal"
         height="40">
  </a>
</p>

<p align="center">
  Prefer using your phone? Scan the QR code:
</p>

<p align="center">
  <a href="https://buymeacoffee.com/vnmatt">
    <img src="https://github.com/gordon-matt/MyResume/blob/gh-pages/assets/images/BuyMeACoffee_QR.png"
         alt="Buy Me a Coffee QR Code"
         width="180">
  </a>
</p>

<img src="https://komarev.com/ghpvc/?username=gordon-matt&label=GitHub%20Hits%20Since%202025-06-01%3A%20&color=ff0000&style=flat" alt="gordon-matt" />

