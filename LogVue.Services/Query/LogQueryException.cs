namespace LogVue.Services.Query;

/// <summary>
/// A query the user typed could not be understood. The message is written for the person who
/// typed it, since it is shown verbatim under the search box.
/// </summary>
public sealed class LogQueryException(string message) : Exception(message);