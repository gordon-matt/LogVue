namespace LogVue.Tests.Providers;

/// <summary>
/// A test that needs a database server, skipped unless <paramref name="variable"/> holds a
/// connection string for one.
/// </summary>
public sealed class ProviderFactAttribute : FactAttribute
{
    public ProviderFactAttribute(string variable)
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(variable)))
        {
            Skip = $"Set {variable} to a server connection string to run this test.";
        }
    }
}
