using Heroicsoft.NetForge.Components.Navigation;

namespace LogVue;

/// <summary>Puts the dashboard on the host menu. The host places it with <c>AddLogVue()</c>.</summary>
public sealed class LogVueMenuContributor(IOptions<LogVueOptions> options) : INetForgeMenuContributor
{
    public string Name => "LogVue";

    public string DefaultMenu => "Maintenance";

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
        public INetForgeMenuGroupBuilder AddLogVue(Action<NetForgeMenuItemOptions>? configure = null) =>
            menu.AddContributor("LogVue", configure);
    }
}