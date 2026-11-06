using Heroicsoft.NetForge.Components.Navigation;

namespace LogVue;

/// <summary>Puts the dashboard on the host menu. The host places it with <c>AddLogVue()</c>.</summary>
public sealed class LogVueMenuContributor(IOptions<LogVueOptions> options) : INetForgeMenuContributor
{
    /// <summary>
    /// Gets the name.
    /// </summary>
    public string Name => "LogVue";

    /// <summary>
    /// Gets the default menu.
    /// </summary>
    public string DefaultMenu => "Maintenance";

    /// <summary>
    /// Performs the contribute operation.
    /// </summary>
    /// <param name="group">The group.</param>
    /// <param name="configure">The configure.</param>
    public void Contribute(INetForgeMenuGroupBuilder group, Action<NetForgeMenuItemOptions>? configure)
    {
        group.AddCustom(item =>
        {
            item.Title = "LogVue";
            item.Icon = "logs";
            item.Url = options.Value.BasePath;
            configure?.Invoke(item);
        });
    }
}

/// <summary>Places the LogVue contributor. The link keeps the library defaults unless <paramref name="configure"/> replaces them.</summary>
public static class LogVueMenuExtensions
{
    extension(INetForgeMenuGroupBuilder menu)
    {
        /// <summary>
        /// Adds the log vue.
        /// </summary>
        /// <param name="configure">The configure.</param>
        /// <returns>The log vue.</returns>
        public INetForgeMenuGroupBuilder AddLogVue(Action<NetForgeMenuItemOptions>? configure = null) =>
            menu.AddContributor("LogVue", configure);
    }
}