using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Jfresolve.Tmdb.Internal;
using Jfresolve.Tmdb.Models;
using Jfresolve.Tmdb.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jfresolve.Tmdb.Services;

/// <summary>
/// Thin wrapper around the TMDB REST API that surfaces strongly typed models.
/// The client is intentionally dependency-light so it can be embedded into any application.
/// </summary>
public sealed class TmdbClient
{
    private readonly HttpClient _httpClient;
    private readonly TmdbClientOptions _options;
    private readonly ILogger<TmdbClient> _logger;

    public TmdbClient(HttpClient httpClient, TmdbClientOptions options, ILogger<TmdbClient>? logger = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new ArgumentException("TMDB API key is required", nameof(options));
        }

        _logger = logger ?? NullLogger<TmdbClient>.Instance;
        var baseUrl = _options.BaseUrl.EndsWith('/') ? _options.BaseUrl : _options.BaseUrl + "/";
        _httpClient.BaseAddress ??= new Uri(baseUrl, UriKind.Absolute);
    }

    /// <summary>
    /// Executes a search across movies and series and returns normalized metadata.
    /// </summary>
    public async Task<IReadOnlyList<TmdbMetadata>> SearchAsync(
        string query,
        TmdbSearchOptions? searchOptions = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("Search query cannot be empty", nameof(query));
        }

        var options = searchOptions ?? TmdbSearchOptions.Default;
        var requestUrl = QueryStringBuilder.Build(
            CombineBase("search/multi"),
            new Dictionary<string, string?>
            {
                ["api_key"] = _options.ApiKey,
                ["query"] = query,
                ["include_adult"] = options.IncludeAdult ? "true" : "false",
                ["language"] = options.Language ?? _options.Language,
                ["region"] = options.Region ?? _options.Region,
            });

        using var responseStream = await SendAsync(requestUrl, cancellationToken).ConfigureAwait(false);
        using var document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken).ConfigureAwait(false);

        var results = new List<TmdbMetadata>();

        if (!document.RootElement.TryGetProperty("results", out var resultsElement))
        {
            return results;
        }

        foreach (var element in resultsElement.EnumerateArray())
        {
            if (!element.TryGetProperty("media_type", out var mediaTypeProperty))
            {
                continue;
            }

            var mediaType = mediaTypeProperty.GetString();
            if (!string.Equals(mediaType, "movie", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(mediaType, "tv", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var isAdult = element.TryGetProperty("adult", out var adultProperty) && adultProperty.GetBoolean();
            if (!options.IncludeAdult && isAdult)
            {
                continue;
            }

            var releaseDate = mediaType == "movie"
                ? element.TryGetProperty("release_date", out var releaseProperty) ? releaseProperty.GetString() : null
                : element.TryGetProperty("first_air_date", out var airProperty) ? airProperty.GetString() : null;

            if (!options.IncludeUnreleased)
            {
                var release = DateParser.ParseDate(releaseDate);
                if (release.HasValue)
                {
                    var buffer = options.UnreleasedBufferDays <= 0
                        ? TimeSpan.Zero
                        : TimeSpan.FromDays(options.UnreleasedBufferDays);

                    if (release.Value.UtcDateTime > DateTime.UtcNow.Add(buffer))
                    {
                        continue;
                    }
                }
            }

            if (options.Year.HasValue)
            {
                var year = DateParser.YearFromDate(releaseDate);
                if (year != options.Year.Value)
                {
                    continue;
                }
            }

            var tmdbId = element.GetProperty("id").GetInt32();
            var title = mediaType == "movie"
                ? element.TryGetProperty("title", out var titleProperty) ? titleProperty.GetString() : null
                : element.TryGetProperty("name", out var nameProperty) ? nameProperty.GetString() : null;

            if (string.IsNullOrWhiteSpace(title))
            {
                continue;
            }

            var kind = mediaType == "movie" ? MediaKind.Movie : MediaKind.Series;
            var overview = element.TryGetProperty("overview", out var overviewProperty) ? overviewProperty.GetString() : null;
            var posterPath = element.TryGetProperty("poster_path", out var posterProperty) ? posterProperty.GetString() : null;
            var backdropPath = element.TryGetProperty("backdrop_path", out var backdropProperty) ? backdropProperty.GetString() : null;
            var popularity = element.TryGetProperty("popularity", out var popularityProperty) ? popularityProperty.GetDouble() : 0d;
            var isAnime = element.TryGetProperty("genre_ids", out var genreProperty) && genreProperty
                .EnumerateArray()
                .Any(genre => genre.GetInt32() == 16);

            var providerIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["tmdb"] = tmdbId.ToString(CultureInfo.InvariantCulture)
            };

            if (options.FetchExternalIds)
            {
                var imdbId = await FetchImdbIdAsync(kind, tmdbId, options.Language, cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(imdbId))
                {
                    providerIds["imdb"] = imdbId!;
                }
            }

            results.Add(new TmdbMetadata(
                tmdbId,
                title!,
                kind,
                overview,
                DateParser.YearFromDate(releaseDate),
                posterPath,
                backdropPath,
                popularity,
                isAdult,
                isAnime,
                providerIds));

            if (options.MaxResults.HasValue && results.Count >= options.MaxResults.Value)
            {
                break;
            }
        }

        return results;
    }

    /// <summary>
    /// Fetches detailed metadata for a single movie including external IDs.
    /// </summary>
    public async Task<TmdbMovieDetails?> GetMovieDetailsAsync(int tmdbId, CancellationToken cancellationToken = default)
    {
        var requestUrl = QueryStringBuilder.Build(
            CombineBase($"movie/{tmdbId}"),
            new Dictionary<string, string?>
            {
                ["api_key"] = _options.ApiKey,
                ["append_to_response"] = "external_ids",
                ["language"] = _options.Language,
            });

        using var responseStream = await SendAsync(requestUrl, cancellationToken).ConfigureAwait(false);
        using var document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken).ConfigureAwait(false);

        var metadata = MaterializeMetadata(document.RootElement, MediaKind.Movie);
        if (metadata == null)
        {
            return null;
        }

        var runtime = document.RootElement.TryGetProperty("runtime", out var runtimeProperty) && runtimeProperty.TryGetInt32(out var minutes)
            ? minutes
            : (int?)null;
        var tagline = document.RootElement.TryGetProperty("tagline", out var taglineProperty) ? taglineProperty.GetString() : null;

        var imdbId = document.RootElement.TryGetProperty("external_ids", out var externalIds)
            && externalIds.TryGetProperty("imdb_id", out var imdbProperty)
            ? imdbProperty.GetString()
            : null;

        if (!string.IsNullOrWhiteSpace(imdbId))
        {
            metadata = metadata with
            {
                ExternalIdentifiers = new Dictionary<string, string>(metadata.ExternalIdentifiers)
                {
                    ["imdb"] = imdbId!
                }
            };
        }

        return new TmdbMovieDetails(metadata, runtime, tagline);
    }

    /// <summary>
    /// Retrieves series metadata and optionally expands seasons/episodes.
    /// </summary>
    public async Task<TmdbSeriesDetails?> GetSeriesDetailsAsync(
        int tmdbId,
        TmdbSeriesOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var requestUrl = QueryStringBuilder.Build(
            CombineBase($"tv/{tmdbId}"),
            new Dictionary<string, string?>
            {
                ["api_key"] = _options.ApiKey,
                ["append_to_response"] = "external_ids",
                ["language"] = options?.Language ?? _options.Language,
            });

        using var responseStream = await SendAsync(requestUrl, cancellationToken).ConfigureAwait(false);
        using var document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken).ConfigureAwait(false);

        var metadata = MaterializeMetadata(document.RootElement, MediaKind.Series);
        if (metadata == null)
        {
            return null;
        }

        var imdbId = document.RootElement.TryGetProperty("external_ids", out var externalIds)
            && externalIds.TryGetProperty("imdb_id", out var imdbProperty)
            ? imdbProperty.GetString()
            : null;

        if (!string.IsNullOrWhiteSpace(imdbId))
        {
            metadata = metadata with
            {
                ExternalIdentifiers = new Dictionary<string, string>(metadata.ExternalIdentifiers)
                {
                    ["imdb"] = imdbId!
                }
            };
        }

        var seasons = new List<TmdbSeason>();
        if (document.RootElement.TryGetProperty("seasons", out var seasonsElement))
        {
            foreach (var seasonElement in seasonsElement.EnumerateArray())
            {
                if (!seasonElement.TryGetProperty("season_number", out var seasonNumberProperty) ||
                    !seasonNumberProperty.TryGetInt32(out var seasonNumber))
                {
                    continue;
                }

                if (seasonNumber == 0 && options?.IncludeSpecials == false)
                {
                    continue;
                }

                var season = await FetchSeasonAsync(tmdbId, seasonNumber, options, cancellationToken).ConfigureAwait(false);
                if (season != null)
                {
                    seasons.Add(season);
                }
            }
        }

        return new TmdbSeriesDetails(metadata, seasons);
    }

    /// <summary>
    /// Downloads a page of popular items for the given <see cref="MediaKind"/>.
    /// </summary>
    public async Task<IReadOnlyList<TmdbMetadata>> GetPopularAsync(
        MediaKind kind,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        var segment = kind == MediaKind.Movie ? "movie/popular" : "tv/popular";
        var requestUrl = QueryStringBuilder.Build(
            CombineBase(segment),
            new Dictionary<string, string?>
            {
                ["api_key"] = _options.ApiKey,
                ["language"] = _options.Language,
                ["region"] = _options.Region,
                ["page"] = page.ToString(CultureInfo.InvariantCulture),
            });

        using var responseStream = await SendAsync(requestUrl, cancellationToken).ConfigureAwait(false);
        using var document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (!document.RootElement.TryGetProperty("results", out var resultsElement))
        {
            return Array.Empty<TmdbMetadata>();
        }

        var results = new List<TmdbMetadata>();
        foreach (var element in resultsElement.EnumerateArray())
        {
            var metadata = MaterializeMetadata(element, kind);
            if (metadata != null)
            {
                results.Add(metadata);
            }
        }

        return results;
    }

    /// <summary>
    /// Converts a poster/backdrop path into an absolute URI.
    /// </summary>
    public Uri? BuildImageUrl(string? relativePath, bool backdrop = false)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return null;
        }

        var size = backdrop ? _options.BackdropSize : _options.PosterSize;
        var baseUrl = _options.ImageBaseUrl.TrimEnd('/');
        relativePath = relativePath.TrimStart('/');
        return new Uri($"{baseUrl}/{size}/{relativePath}", UriKind.Absolute);
    }

    private async Task<Stream> SendAsync(string requestUrl, CancellationToken cancellationToken)
    {
        _logger.LogDebug("TMDB request: {Url}", requestUrl);

        var response = await _httpClient.GetAsync(requestUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
    }

    private async Task<string?> FetchImdbIdAsync(MediaKind kind, int tmdbId, string? language, CancellationToken cancellationToken)
    {
        var segment = kind == MediaKind.Movie ? "movie" : "tv";
        var requestUrl = QueryStringBuilder.Build(
            CombineBase($"{segment}/{tmdbId}/external_ids"),
            new Dictionary<string, string?>
            {
                ["api_key"] = _options.ApiKey,
                ["language"] = language ?? _options.Language,
            });

        using var responseStream = await SendAsync(requestUrl, cancellationToken).ConfigureAwait(false);
        using var document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken).ConfigureAwait(false);

        return document.RootElement.TryGetProperty("imdb_id", out var imdbProperty)
            ? imdbProperty.GetString()
            : null;
    }

    private async Task<TmdbSeason?> FetchSeasonAsync(
        int seriesId,
        int seasonNumber,
        TmdbSeriesOptions? options,
        CancellationToken cancellationToken)
    {
        var requestUrl = QueryStringBuilder.Build(
            CombineBase($"tv/{seriesId}/season/{seasonNumber}"),
            new Dictionary<string, string?>
            {
                ["api_key"] = _options.ApiKey,
                ["language"] = options?.Language ?? _options.Language,
            });

        using var responseStream = await SendAsync(requestUrl, cancellationToken).ConfigureAwait(false);
        using var document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (!document.RootElement.TryGetProperty("episodes", out var episodesElement))
        {
            return null;
        }

        var episodes = new List<TmdbEpisode>();
        foreach (var episodeElement in episodesElement.EnumerateArray())
        {
            if (!episodeElement.TryGetProperty("episode_number", out var episodeNumberProperty) ||
                !episodeNumberProperty.TryGetInt32(out var episodeNumber))
            {
                continue;
            }

            var title = episodeElement.TryGetProperty("name", out var nameProperty) ? nameProperty.GetString() : null;
            if (string.IsNullOrWhiteSpace(title))
            {
                continue;
            }

            episodes.Add(new TmdbEpisode(
                seasonNumber,
                episodeNumber,
                title!,
                episodeElement.TryGetProperty("overview", out var overviewProperty) ? overviewProperty.GetString() : null,
                DateParser.ParseDate(episodeElement.TryGetProperty("air_date", out var airProperty) ? airProperty.GetString() : null),
                null,
                episodeElement.TryGetProperty("still_path", out var stillProperty) ? stillProperty.GetString() : null));
        }

        return new TmdbSeason(seasonNumber, episodes);
    }

    private static string CombineBase(string relative)
    {
        if (string.IsNullOrWhiteSpace(relative))
        {
            throw new ArgumentException("Relative URL cannot be empty", nameof(relative));
        }

        return relative.TrimStart('/');
    }

    private TmdbMetadata? MaterializeMetadata(JsonElement element, MediaKind? explicitKind)
    {
        MediaKind kind;
        if (explicitKind.HasValue)
        {
            kind = explicitKind.Value;
        }
        else if (element.TryGetProperty("media_type", out var mediaTypeProperty))
        {
            var mediaType = mediaTypeProperty.GetString();
            if (string.Equals(mediaType, "movie", StringComparison.OrdinalIgnoreCase))
            {
                kind = MediaKind.Movie;
            }
            else if (string.Equals(mediaType, "tv", StringComparison.OrdinalIgnoreCase))
            {
                kind = MediaKind.Series;
            }
            else
            {
                return null;
            }
        }
        else
        {
            return null;
        }

        var tmdbId = element.TryGetProperty("id", out var idProperty) ? idProperty.GetInt32() : (int?)null;
        if (!tmdbId.HasValue)
        {
            return null;
        }

        var title = kind == MediaKind.Movie
            ? element.TryGetProperty("title", out var titleProperty) ? titleProperty.GetString() : null
            : element.TryGetProperty("name", out var nameProperty) ? nameProperty.GetString() : null;

        if (string.IsNullOrWhiteSpace(title))
        {
            return null;
        }

        var releaseDate = kind == MediaKind.Movie
            ? element.TryGetProperty("release_date", out var releaseProperty) ? releaseProperty.GetString() : null
            : element.TryGetProperty("first_air_date", out var airProperty) ? airProperty.GetString() : null;

        var isAdult = element.TryGetProperty("adult", out var adultProperty) && adultProperty.GetBoolean();
        var isAnime = element.TryGetProperty("genres", out var genresProperty)
            ? genresProperty.EnumerateArray().Any(genre => genre.TryGetProperty("id", out var idProp) && idProp.GetInt32() == 16)
            : element.TryGetProperty("genre_ids", out var genreIdsProperty) && genreIdsProperty.EnumerateArray().Any(value => value.GetInt32() == 16);

        var providerIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["tmdb"] = tmdbId.Value.ToString(CultureInfo.InvariantCulture)
        };

        if (element.TryGetProperty("external_ids", out var externalIds) && externalIds.TryGetProperty("imdb_id", out var imdbProperty))
        {
            var imdbId = imdbProperty.GetString();
            if (!string.IsNullOrWhiteSpace(imdbId))
            {
                providerIds["imdb"] = imdbId!;
            }
        }

        return new TmdbMetadata(
            tmdbId.Value,
            title!,
            kind,
            element.TryGetProperty("overview", out var overviewProperty) ? overviewProperty.GetString() : null,
            DateParser.YearFromDate(releaseDate),
            element.TryGetProperty("poster_path", out var posterProperty) ? posterProperty.GetString() : null,
            element.TryGetProperty("backdrop_path", out var backdropProperty) ? backdropProperty.GetString() : null,
            element.TryGetProperty("popularity", out var popularityProperty) ? popularityProperty.GetDouble() : 0d,
            isAdult,
            isAnime,
            providerIds);
    }
}
