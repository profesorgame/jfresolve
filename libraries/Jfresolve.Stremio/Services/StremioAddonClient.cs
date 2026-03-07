using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jfresolve.Stremio.Models;
using Jfresolve.Stremio.Options;
using Jfresolve.Stremio.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jfresolve.Stremio.Services;

/// <summary>
/// Lightweight client for consuming Stremio add-on endpoints.
/// </summary>
public sealed class StremioAddonClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<StremioAddonClient> _logger;
    private readonly string _baseUrl;

    public StremioAddonClient(HttpClient httpClient, StremioAddonOptions options, ILogger<StremioAddonClient>? logger = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (string.IsNullOrWhiteSpace(options.ManifestUrl))
        {
            throw new ArgumentException("Manifest URL is required", nameof(options));
        }

        _baseUrl = ManifestUrlUtility.Normalize(options.ManifestUrl);
        _logger = logger ?? NullLogger<StremioAddonClient>.Instance;

        if (options.RequestTimeout.HasValue)
        {
            _httpClient.Timeout = options.RequestTimeout.Value;
        }
    }

    public async Task<StremioManifest> GetManifestAsync(CancellationToken cancellationToken = default)
    {
        var url = $"{_baseUrl}/manifest.json";
        _logger.LogDebug("Fetching Stremio manifest from {Url}", url);
        using var response = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

        var root = document.RootElement;
        var id = root.TryGetProperty("id", out var idProperty) ? idProperty.GetString() ?? string.Empty : string.Empty;
        var name = root.TryGetProperty("name", out var nameProperty) ? nameProperty.GetString() ?? string.Empty : string.Empty;
        var version = root.TryGetProperty("version", out var versionProperty) ? versionProperty.GetString() ?? "1.0.0" : "1.0.0";
        var description = root.TryGetProperty("description", out var descriptionProperty) ? descriptionProperty.GetString() : null;

        var resources = new List<string>();
        if (root.TryGetProperty("resources", out var resourcesElement))
        {
            if (resourcesElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in resourcesElement.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                    {
                        resources.Add(item.GetString()!);
                    }
                }
            }
            else if (resourcesElement.ValueKind == JsonValueKind.String)
            {
                resources.Add(resourcesElement.GetString()!);
            }
        }

        var types = new List<string>();
        if (root.TryGetProperty("types", out var typesElement))
        {
            foreach (var item in typesElement.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    types.Add(item.GetString()!);
                }
            }
        }

        return new StremioManifest(id, name, version, description, resources, types);
    }

    public async Task<IReadOnlyList<StremioCatalogItem>> GetCatalogAsync(
        string type,
        string id,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            throw new ArgumentException("Catalog type is required", nameof(type));
        }

        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Catalog identifier is required", nameof(id));
        }

        var url = ManifestUrlUtility.BuildCatalogPath(_baseUrl, type, id);
        _logger.LogDebug("Fetching Stremio catalog from {Url}", url);
        using var response = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

        var items = new List<StremioCatalogItem>();
        if (document.RootElement.TryGetProperty("metas", out var metasElement))
        {
            foreach (var meta in metasElement.EnumerateArray())
            {
                var itemId = meta.TryGetProperty("id", out var idProperty) ? idProperty.GetString() : null;
                var itemType = meta.TryGetProperty("type", out var typeProperty) ? typeProperty.GetString() : null;
                var name = meta.TryGetProperty("name", out var nameProperty) ? nameProperty.GetString() : null;
                if (string.IsNullOrWhiteSpace(itemId) || string.IsNullOrWhiteSpace(itemType) || string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                var poster = meta.TryGetProperty("poster", out var posterProperty) ? posterProperty.GetString() : null;
                var background = meta.TryGetProperty("background", out var backgroundProperty) ? backgroundProperty.GetString() : null;

                items.Add(new StremioCatalogItem(itemId!, itemType!, name!, poster, background));
            }
        }

        return items;
    }

    public async Task<IReadOnlyList<StremioStream>> GetStreamsAsync(
        StremioStreamRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Type))
        {
            throw new ArgumentException("Stream type is required", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Id))
        {
            throw new ArgumentException("Stream identifier is required", nameof(request));
        }

        var url = ManifestUrlUtility.BuildStreamPath(_baseUrl, request.Type, request.Id, request.Season, request.Episode);
        _logger.LogDebug("Fetching Stremio streams from {Url}", url);
        using var response = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

        var streams = new List<StremioStream>();
        if (document.RootElement.TryGetProperty("streams", out var streamsElement))
        {
            foreach (var element in streamsElement.EnumerateArray())
            {
                var urlProperty = element.TryGetProperty("url", out var rawUrl) ? rawUrl.GetString() : null;
                if (string.IsNullOrWhiteSpace(urlProperty))
                {
                    continue;
                }

                var title = element.TryGetProperty("title", out var titleProperty) ? titleProperty.GetString() : null;
                var description = element.TryGetProperty("description", out var descriptionProperty) ? descriptionProperty.GetString() : null;

                IReadOnlyDictionary<string, object?>? hints = null;
                if (element.TryGetProperty("behaviorHints", out var hintsElement) && hintsElement.ValueKind == JsonValueKind.Object)
                {
                    var dictionary = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                    foreach (var property in hintsElement.EnumerateObject())
                    {
                        dictionary[property.Name] = ExtractJsonValue(property.Value);
                    }

                    hints = dictionary;
                }

                streams.Add(new StremioStream(urlProperty!, title, description, hints));
            }
        }

        return streams;
    }

    private static object? ExtractJsonValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Array => element.EnumerateArray().Select(ExtractJsonValue).ToArray(),
            JsonValueKind.Object => element
                .EnumerateObject()
                .ToDictionary(p => p.Name, p => ExtractJsonValue(p.Value), StringComparer.OrdinalIgnoreCase),
            JsonValueKind.Null => null,
            _ => element.GetRawText()
        };
    }
}
