using System;

namespace Jfresolve.Stremio.Utilities;

internal static class ManifestUrlUtility
{
    public static string Normalize(string manifestUrl)
    {
        if (string.IsNullOrWhiteSpace(manifestUrl))
        {
            throw new ArgumentException("Manifest URL cannot be empty", nameof(manifestUrl));
        }

        var sanitized = manifestUrl
            .Replace("manifest.json", string.Empty, StringComparison.OrdinalIgnoreCase)
            .TrimEnd('/')
            .Replace("stremio://", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("https://", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("http://", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim('/');

        return $"https://{sanitized}";
    }

    public static string BuildStreamPath(string normalizedBase, string type, string id, int? season, int? episode)
    {
        normalizedBase = normalizedBase.TrimEnd('/');

        if (type.Equals("series", StringComparison.OrdinalIgnoreCase))
        {
            if (!season.HasValue || !episode.HasValue)
            {
                throw new ArgumentException("Season and episode are required for series streams.");
            }

            return $"{normalizedBase}/stream/series/{id}:{season.Value}:{episode.Value}.json";
        }

        if (type.Equals("movie", StringComparison.OrdinalIgnoreCase))
        {
            return $"{normalizedBase}/stream/movie/{id}.json";
        }

        return $"{normalizedBase}/stream/{type}/{id}.json";
    }

    public static string BuildCatalogPath(string normalizedBase, string type, string id)
    {
        normalizedBase = normalizedBase.TrimEnd('/');
        return $"{normalizedBase}/catalog/{type}/{id}.json";
    }
}
