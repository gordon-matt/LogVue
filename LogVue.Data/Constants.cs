namespace LogVue;

public static class Constants
{
    /// <summary>
    /// LogVue usually shares a database with the host application, so it keeps its own migration
    /// history. Using EF's default table would make each side think the other's migrations are
    /// missing.
    /// </summary>
    public const string MigrationsHistoryTable = "__LogVueMigrationsHistory";

    /// <summary>
    /// Table names are prefixed rather than schema-qualified because SQLite and MySQL have no
    /// schemas, and one naming scheme across all providers is easier to find in a shared database.
    /// </summary>
    public static class Tables
    {
        public const string Entries = "LogVueEntries";
        public const string EntryProperties = "LogVueEntryProperties";
        public const string SavedQueries = "LogVueSavedQueries";
    }

    public static class MaxLengths
    {
        /// <summary>Kept short enough to sit in a composite index on every provider.</summary>
        public const int PropertyName = 256;

        public const int SavedQueryName = 128;
        public const int SavedQueryText = 4000;
    }

    public static class DatabaseProviders
    {
        public const string MySql = "MySql";
        public const string Npgsql = "Npgsql";
        public const string Sqlite = "Sqlite";
        public const string SqlServer = "SqlServer";
    }
}