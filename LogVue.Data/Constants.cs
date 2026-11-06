namespace LogVue;

/// <summary>
/// Provides constants.
/// </summary>
public static class Constants
{
    /// <summary>
    /// LogVue usually shares a database with the host application, so it keeps its own migration
    /// history. Using EF's default table would make each side think the other's migrations are
    /// missing.
    /// </summary>
    public const string MigrationsHistoryTable = "__LogVueMigrationsHistory";

    /// <summary>Database schema for providers that support one (SQL Server and PostgreSQL).</summary>
    public const string Schema = "logvue";

    /// <summary>
    /// Prepended to every table name on providers without schemas. In MySQL a schema is a database,
    /// and SQLite has none, so the tables stay unqualified and are prefixed instead.
    /// </summary>
    public const string TablePrefix = "LogVue_";

    /// <summary>
    /// Unqualified table names. The schema or <see cref="TablePrefix"/> is applied by the context.
    /// </summary>
    public static class Tables
    {
        /// <summary>
        /// The entries constant.
        /// </summary>
        public const string Entries = "Entries";
        /// <summary>
        /// The entry properties constant.
        /// </summary>
        public const string EntryProperties = "EntryProperties";
        /// <summary>
        /// The saved queries constant.
        /// </summary>
        public const string SavedQueries = "SavedQueries";
    }

    /// <summary>
    /// Provides max lengths.
    /// </summary>
    public static class MaxLengths
    {
        /// <summary>Kept short enough to sit in a composite index on every provider.</summary>
        public const int PropertyName = 256;

        /// <summary>
        /// The saved query name constant.
        /// </summary>
        public const int SavedQueryName = 128;
        /// <summary>
        /// The saved query text constant.
        /// </summary>
        public const int SavedQueryText = 4000;
    }

    /// <summary>
    /// Provides database providers.
    /// </summary>
    public static class DatabaseProviders
    {
        /// <summary>
        /// The my SQL constant.
        /// </summary>
        public const string MySql = "MySql";
        /// <summary>
        /// The npgsql constant.
        /// </summary>
        public const string Npgsql = "Npgsql";
        /// <summary>
        /// The sqlite constant.
        /// </summary>
        public const string Sqlite = "Sqlite";
        /// <summary>
        /// The SQL server constant.
        /// </summary>
        public const string SqlServer = "SqlServer";
    }
}