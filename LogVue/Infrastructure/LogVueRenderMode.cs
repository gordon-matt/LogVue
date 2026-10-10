using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace LogVue.Infrastructure;

internal static class LogVueRenderMode
{
    /// <summary>
    /// Not prerendered: the page is a live view of the database, so rendering it once on the
    /// server only to query everything again when the circuit connects would double the load.
    /// </summary>
    public static IComponentRenderMode Interactive { get; } = new InteractiveServerRenderMode(prerender: false);
}
