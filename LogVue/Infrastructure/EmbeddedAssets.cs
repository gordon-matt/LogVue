using System.Collections.Concurrent;
using System.Security.Cryptography;
using Microsoft.Net.Http.Headers;

namespace LogVue.Infrastructure;

/// <summary>
/// The dashboard's stylesheet and scripts, compiled into the assembly. URLs carry a content hash,
/// so browsers can cache them indefinitely and still pick up a new version after an upgrade.
/// </summary>
internal static class EmbeddedAssets
{
    private const string ResourcePrefix = "LogVue.Assets.";

    private static readonly ConcurrentDictionary<string, Asset?> Cache = new(StringComparer.OrdinalIgnoreCase);

    public static string Url(string name) => Get(name) is { } asset
        ? $"_assets/{name}?v={asset.Version}"
        : $"_assets/{name}";

    public static IResult Serve(string name, HttpContext context)
    {
        if (Get(name) is not { } asset)
        {
            return Results.NotFound();
        }

        var headers = context.Response.GetTypedHeaders();
        headers.ETag = asset.ETag;
        headers.CacheControl = context.Request.Query.ContainsKey("v")
            ? new CacheControlHeaderValue { Public = true, MaxAge = TimeSpan.FromDays(365), Extensions = { new NameValueHeaderValue("immutable") } }
            : new CacheControlHeaderValue { NoCache = true };

        var requestETags = context.Request.GetTypedHeaders().IfNoneMatch;
        return requestETags.Any(tag => tag.Compare(asset.ETag, useStrongComparison: false))
            ? Results.StatusCode(StatusCodes.Status304NotModified)
            : Results.Bytes(asset.Content, asset.ContentType);
    }

    private static Asset? Get(string name) => Cache.GetOrAdd(name, Load);

    private static Asset? Load(string name)
    {
        using var stream = typeof(EmbeddedAssets).Assembly.GetManifestResourceStream(ResourcePrefix + name);

        if (stream is null)
        {
            return null;
        }

        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        byte[] content = buffer.ToArray();

        string version = Convert.ToHexStringLower(SHA256.HashData(content))[..12];

        return new Asset(content, ContentTypeFor(name), new EntityTagHeaderValue($"\"{version}\""), version);
    }

    private static string ContentTypeFor(string name) => Path.GetExtension(name).ToLowerInvariant() switch
    {
        ".css" => "text/css; charset=utf-8",
        ".js" => "text/javascript; charset=utf-8",
        ".svg" => "image/svg+xml",
        _ => "application/octet-stream",
    };

    private sealed record Asset(byte[] Content, string ContentType, EntityTagHeaderValue ETag, string Version);
}