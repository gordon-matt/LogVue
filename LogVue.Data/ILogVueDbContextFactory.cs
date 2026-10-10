namespace LogVue.Data;

/// <summary>
/// LogVue's own context factory. It deliberately extends rather than replaces Extenso's
/// <see cref="IDbContextFactory"/>: host applications built on Extenso register that interface for
/// their own context, and LogVue must not take it over. LogVue's repositories are constructed
/// against this type explicitly instead.
/// </summary>
public interface ILogVueDbContextFactory : IDbContextFactory
{
    /// <summary>One of <see cref="Constants.DatabaseProviders"/>, shown on the dashboard.</summary>
    string ProviderName { get; }
}
