using System.Data.Common;
using Extenso.Data.Entity;
using LogVue.Data;
using LogVue.Data.Entities;
using LogVue.Services.LogEntries;
using LogVue.Services.SavedQueries;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace LogVue.Tests;

/// <summary>
/// A private, freshly migrated database. Each test gets its own, so tests can run in parallel,
/// and server databases are dropped again afterwards.
/// </summary>
public sealed class TestDatabase : IDisposable
{
    private readonly IDisposable? keepAlive;
    private readonly bool dropOnDispose;

    private TestDatabase(ILogVueDbContextFactory factory, ILogVueQueryDialect dialect, IDisposable? keepAlive, bool dropOnDispose)
    {
        Factory = factory;
        Dialect = dialect;
        this.keepAlive = keepAlive;
        this.dropOnDispose = dropOnDispose;

        using var context = Factory.GetContext();
        context.Database.Migrate();
    }

    public ILogVueDbContextFactory Factory { get; }

    public ILogVueQueryDialect Dialect { get; }

    public LogVueOptions Options { get; } = new();

    public IRepository<LogEntry> Entries => new EntityFrameworkRepository<LogEntry>(Factory);

    public IRepository<LogEntryProperty> Properties => new EntityFrameworkRepository<LogEntryProperty>(Factory);

    /// <summary>
    /// In memory. The open connection keeps the shared in-memory database alive between contexts.
    /// </summary>
    public static TestDatabase CreateSqlite()
    {
        string connectionString = $"Data Source=logvue-tests-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";

        var keepAlive = new SqliteConnection(connectionString);
        keepAlive.Open();

        return new TestDatabase(
            new Data.Sqlite.LogVueDbContextFactory(connectionString),
            new DefaultQueryDialect(),
            keepAlive,
            dropOnDispose: false);
    }

    /// <summary>A uniquely named database on the server in <paramref name="serverConnectionString"/>.</summary>
    public static TestDatabase CreateOnServer(
        string serverConnectionString,
        Func<string, ILogVueDbContextFactory> createFactory,
        ILogVueQueryDialect dialect)
    {
        var builder = new DbConnectionStringBuilder { ConnectionString = serverConnectionString };
        builder["Database"] = $"logvue_tests_{Guid.NewGuid():N}"[..28];

        return new TestDatabase(createFactory(builder.ConnectionString), dialect, keepAlive: null, dropOnDispose: true);
    }

    public LogEntryService CreateLogEntryService() => new(
        NullLogger<LogEntryService>.Instance,
        Microsoft.Extensions.Options.Options.Create(Options),
        Dialect,
        Factory,
        Entries,
        Properties);

    public SavedQueryService CreateSavedQueryService() => new(
        Microsoft.Extensions.Options.Options.Create(Options),
        Dialect,
        new EntityFrameworkRepository<SavedQuery>(Factory));

    public async Task<LogEntry> AddAsync(
        string message,
        LogVueLevel level = LogVueLevel.Information,
        DateTime? timestamp = null,
        string? exception = null,
        params (string Name, string? Value)[] properties)
    {
        var entry = new LogEntry
        {
            Timestamp = timestamp ?? DateTime.UtcNow,
            Level = level,
            Message = message,
            MessageTemplate = message,
            Exception = exception,
        };

        foreach (var (name, value) in properties)
        {
            entry.Properties.Add(new LogEntryProperty { Name = name, Value = value });
        }

        return await Entries.InsertAsync(entry);
    }

    public void Dispose()
    {
        if (dropOnDispose)
        {
            using var context = Factory.GetContext();
            context.Database.EnsureDeleted();
        }

        keepAlive?.Dispose();
    }
}