using LogVue.Components;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;

namespace LogVue;

public static class EndpointRouteBuilderExtensions
{
    extension(IEndpointRouteBuilder endpoints)
    {
        /// <summary>
        /// Mounts the dashboard at <see cref="LogVueOptions.BasePath"/>. The returned group covers
        /// every LogVue endpoint, including the Blazor circuit, so it is the place to require
        /// authorization:
        /// <code>app.MapLogVue().RequireAuthorization("Administrators");</code>
        /// </summary>
        public RouteGroupBuilder MapLogVue()
        {
            var options = endpoints.ServiceProvider.GetRequiredService<IOptions<LogVueOptions>>().Value;

            var group = endpoints.MapGroup(options.BasePath);

            group.MapGet("_assets/{name}", EmbeddedAssets.Serve).ExcludeFromDescription();

            // The circuit script is requested relative to the dashboard's <base href>, which keeps
            // its hub connection under the base path too and away from any Blazor app the host
            // runs itself.
            group.MapGet("_framework/blazor.web.js", ServeBlazorScript).ExcludeFromDescription();

            // Antiforgery is disabled because the dashboard is entirely interactive: it never posts
            // a form, and requiring the middleware would make UseAntiforgery a host prerequisite.
            group.MapRazorComponents<LogVueApp>()
                .AddInteractiveServerRenderMode()
                .DisableAntiforgery();

            return group;
        }
    }

    /// <summary>
    /// Serves the framework's own copy of <c>blazor.web.js</c>, which .NET 10 ships to every web
    /// project as a static web asset. Reading it through the web root file provider finds it both
    /// in development (from the static web assets manifest) and after publishing (in wwwroot).
    /// </summary>
    private static IResult ServeBlazorScript(IWebHostEnvironment environment)
    {
        var file = environment.WebRootFileProvider.GetFileInfo("_framework/blazor.web.js");

        if (!file.Exists)
        {
            return Results.Problem(
                "blazor.web.js was not found in the web root. LogVue needs the host to be built with " +
                "the ASP.NET Core Web SDK, and static web assets to be enabled outside Development " +
                "(they are copied into wwwroot on publish).",
                statusCode: StatusCodes.Status404NotFound);
        }

        return file.PhysicalPath is { } physicalPath
            ? Results.File(physicalPath, "text/javascript")
            : Results.Stream(file.CreateReadStream(), "text/javascript", lastModified: file.LastModified);
    }
}
