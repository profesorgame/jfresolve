using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jfresolve.Strm.Internal;
using Jfresolve.Strm.Models;
using Jfresolve.Strm.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jfresolve.Strm.Services;

/// <summary>
/// Creates STRM files, optional sidecar metadata and NFO files for Jellyfin friendly libraries.
/// </summary>
public sealed class StrmFileGenerator
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly IFileSystem _fileSystem;
    private readonly ILogger<StrmFileGenerator> _logger;
    private readonly ResolvedSettings _defaults;

    public StrmFileGenerator(
        IFileSystem? fileSystem = null,
        ILogger<StrmFileGenerator>? logger = null,
        StrmGenerationSettings? defaults = null)
    {
        _fileSystem = fileSystem ?? new FileSystem();
        _logger = logger ?? NullLogger<StrmFileGenerator>.Instance;
        _defaults = Resolve(defaults ?? new StrmGenerationSettings
        {
            OverwriteExisting = false,
            CreateMetadataSidecars = true,
            CreateEpisodeNfo = true,
            MetadataExtension = ".jfresolve.json"
        });
    }

    public async Task<StrmGenerationResult> CreateMovieAsync(
        MovieStrmRequest request,
        StrmGenerationSettings? overrides = null,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new ArgumentException("Movie title is required", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.StreamUrl))
        {
            throw new ArgumentException("Movie stream URL is required", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.DestinationDirectory))
        {
            throw new ArgumentException("Destination directory is required", nameof(request));
        }

        var settings = MergeSettings(overrides);
        var folderName = request.Year.HasValue
            ? $"{request.Title} ({request.Year.Value})"
            : request.Title;
        var movieFolder = PathHelpers.CombineWithSanitizedName(request.DestinationDirectory, folderName);
        var fileName = PathHelpers.SanitizeName(folderName) + ".strm";
        var strmPath = Path.Combine(movieFolder, fileName);

        var created = new List<string>();
        var skipped = new List<string>();

        PathHelpers.EnsureDirectory(_fileSystem, movieFolder);
        await WriteFileAsync(strmPath, request.StreamUrl + Environment.NewLine, settings, created, skipped, cancellationToken)
            .ConfigureAwait(false);

        if (settings.CreateMetadataSidecars)
        {
            var metadataPath = Path.ChangeExtension(strmPath, settings.MetadataExtension);
            var payload = BuildMetadataPayload(
                "Movie",
                request.Title,
                request.Year,
                request.StreamUrl,
                request.LibraryItemId,
                request.ImdbId,
                request.TmdbId,
                request.ProviderIds,
                request.Metadata);

            await WriteFileAsync(metadataPath, JsonSerializer.Serialize(payload, JsonOptions), settings, created, skipped, cancellationToken)
                .ConfigureAwait(false);
        }

        _logger.LogInformation("Created STRM for movie {Title} at {Path}", request.Title, movieFolder);
        return new StrmGenerationResult(movieFolder, created, skipped);
    }

    public async Task<StrmGenerationResult> CreateSeriesAsync(
        SeriesStrmRequest request,
        StrmGenerationSettings? overrides = null,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new ArgumentException("Series title is required", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.DestinationDirectory))
        {
            throw new ArgumentException("Destination directory is required", nameof(request));
        }

        if (request.Seasons is null || request.Seasons.Count == 0)
        {
            throw new ArgumentException("At least one season is required", nameof(request));
        }

        var settings = MergeSettings(overrides);
        var folderName = request.Year.HasValue
            ? $"{request.Title} ({request.Year.Value})"
            : request.Title;
        var seriesFolder = PathHelpers.CombineWithSanitizedName(request.DestinationDirectory, folderName);

        var created = new List<string>();
        var skipped = new List<string>();

        PathHelpers.EnsureDirectory(_fileSystem, seriesFolder);

        foreach (var season in request.Seasons.OrderBy(season => season.SeasonNumber))
        {
            var seasonFolder = PathHelpers.CombineWithSanitizedName(seriesFolder, $"Season {season.SeasonNumber:D2}");
            PathHelpers.EnsureDirectory(_fileSystem, seasonFolder);

            foreach (var episode in season.Episodes.OrderBy(episode => episode.EpisodeNumber))
            {
                var fileName = BuildEpisodeFileName(request.Title, season.SeasonNumber, episode.EpisodeNumber, episode.Title);
                var strmPath = Path.Combine(seasonFolder, fileName + ".strm");

                await WriteFileAsync(strmPath, episode.StreamUrl + Environment.NewLine, settings, created, skipped, cancellationToken)
                    .ConfigureAwait(false);

                if (settings.CreateMetadataSidecars)
                {
                    var metadataPath = Path.ChangeExtension(strmPath, settings.MetadataExtension);
                    var payload = BuildEpisodeMetadata(
                        request,
                        season,
                        episode,
                        strmPath,
                        episode.Metadata);

                    await WriteFileAsync(metadataPath, JsonSerializer.Serialize(payload, JsonOptions), settings, created, skipped, cancellationToken)
                        .ConfigureAwait(false);
                }

                if (settings.CreateEpisodeNfo)
                {
                    var nfoPath = Path.ChangeExtension(strmPath, ".nfo");
                    var nfoContent = BuildEpisodeNfo(request.Title, season.SeasonNumber, episode.EpisodeNumber, episode.Title);
                    await WriteFileAsync(nfoPath, nfoContent, settings, created, skipped, cancellationToken)
                        .ConfigureAwait(false);
                }
            }
        }

        if (settings.CreateMetadataSidecars)
        {
            var metadataPath = Path.Combine(seriesFolder, "series" + settings.MetadataExtension);
            var payload = BuildMetadataPayload(
                "Series",
                request.Title,
                request.Year,
                null,
                request.LibraryItemId,
                request.ImdbId,
                request.TmdbId,
                request.ProviderIds,
                null);

            await WriteFileAsync(metadataPath, JsonSerializer.Serialize(payload, JsonOptions), settings, created, skipped, cancellationToken)
                .ConfigureAwait(false);
        }

        _logger.LogInformation("Created STRM structure for series {Title} at {Path}", request.Title, seriesFolder);
        return new StrmGenerationResult(seriesFolder, created, skipped);
    }

    private ResolvedSettings MergeSettings(StrmGenerationSettings? overrides)
    {
        if (overrides is null)
        {
            return _defaults;
        }

        return Resolve(new StrmGenerationSettings
        {
            OverwriteExisting = overrides.OverwriteExisting ?? _defaults.OverwriteExisting,
            CreateMetadataSidecars = overrides.CreateMetadataSidecars ?? _defaults.CreateMetadataSidecars,
            CreateEpisodeNfo = overrides.CreateEpisodeNfo ?? _defaults.CreateEpisodeNfo,
            MetadataExtension = overrides.MetadataExtension ?? _defaults.MetadataExtension
        });
    }

    private static ResolvedSettings Resolve(StrmGenerationSettings source)
    {
        var metadataExtension = string.IsNullOrWhiteSpace(source.MetadataExtension)
            ? ".jfresolve.json"
            : source.MetadataExtension.StartsWith('.')
                ? source.MetadataExtension
                : "." + source.MetadataExtension;

        return new ResolvedSettings(
            source.OverwriteExisting ?? false,
            source.CreateMetadataSidecars ?? true,
            source.CreateEpisodeNfo ?? true,
            metadataExtension);
    }

    private async Task WriteFileAsync(
        string path,
        string content,
        ResolvedSettings settings,
        ICollection<string> created,
        ICollection<string> skipped,
        CancellationToken cancellationToken)
    {
        if (_fileSystem.File.Exists(path) && !settings.OverwriteExisting)
        {
            skipped.Add(path);
            return;
        }

        await using var stream = _fileSystem.File.Create(path);
        await using var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: false);
        await writer.WriteAsync(content.AsMemory(), cancellationToken).ConfigureAwait(false);
        created.Add(path);
    }

    private IDictionary<string, object?> BuildMetadataPayload(
        string type,
        string title,
        int? year,
        string? streamUrl,
        Guid? libraryId,
        string? imdbId,
        int? tmdbId,
        IReadOnlyDictionary<string, string>? providerIds,
        IReadOnlyDictionary<string, object?>? metadata)
    {
        var providers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(imdbId))
        {
            providers["imdb"] = imdbId!;
        }

        if (tmdbId.HasValue)
        {
            providers["tmdb"] = tmdbId.Value.ToString(CultureInfo.InvariantCulture);
        }

        if (providerIds != null)
        {
            foreach (var kvp in providerIds)
            {
                providers[kvp.Key] = kvp.Value;
            }
        }

        var payload = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["version"] = "1.0",
            ["type"] = type,
            ["title"] = title,
            ["year"] = year,
            ["createdAt"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            ["libraryId"] = (libraryId ?? CreateDeterministicGuid(type, title, streamUrl)).ToString()
        };

        if (!string.IsNullOrWhiteSpace(streamUrl))
        {
            payload["streamUrl"] = streamUrl;
        }

        if (providers.Count > 0)
        {
            payload["providerIds"] = providers;
        }

        if (metadata != null && metadata.Count > 0)
        {
            payload["metadata"] = metadata;
        }

        return payload;
    }

    private IDictionary<string, object?> BuildEpisodeMetadata(
        SeriesStrmRequest series,
        SeasonStrmRequest season,
        EpisodeStrmRequest episode,
        string strmPath,
        IReadOnlyDictionary<string, object?>? metadata)
    {
        var providers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(series.ImdbId))
        {
            providers["series:imdb"] = series.ImdbId!;
        }

        if (series.TmdbId.HasValue)
        {
            providers["series:tmdb"] = series.TmdbId.Value.ToString(CultureInfo.InvariantCulture);
        }

        if (!string.IsNullOrWhiteSpace(episode.ImdbId))
        {
            providers["imdb"] = episode.ImdbId!;
        }

        if (episode.TmdbId.HasValue)
        {
            providers["tmdb"] = episode.TmdbId.Value.ToString(CultureInfo.InvariantCulture);
        }

        var payload = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["version"] = "1.0",
            ["type"] = "Episode",
            ["title"] = series.Title,
            ["season"] = season.SeasonNumber,
            ["episode"] = episode.EpisodeNumber,
            ["createdAt"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            ["libraryId"] = (episode.LibraryItemId ?? CreateDeterministicGuid("Episode", strmPath, episode.StreamUrl)).ToString(),
            ["streamUrl"] = episode.StreamUrl
        };

        if (providers.Count > 0)
        {
            payload["providerIds"] = providers;
        }

        if (metadata != null && metadata.Count > 0)
        {
            payload["metadata"] = metadata;
        }

        return payload;
    }

    private static string BuildEpisodeFileName(string seriesTitle, int seasonNumber, int episodeNumber, string? episodeTitle)
    {
        var baseName = new StringBuilder();
        baseName.Append(PathHelpers.SanitizeName(seriesTitle));
        baseName.Append(' ');
        baseName.Append($"S{seasonNumber:D2}E{episodeNumber:D2}");
        if (!string.IsNullOrWhiteSpace(episodeTitle))
        {
            baseName.Append(' ');
            baseName.Append(PathHelpers.SanitizeName(episodeTitle));
        }

        return baseName.ToString();
    }

    private static string BuildEpisodeNfo(string seriesTitle, int seasonNumber, int episodeNumber, string? episodeTitle)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
        builder.AppendLine("<episodedetails>");
        var title = string.IsNullOrWhiteSpace(episodeTitle)
            ? $"{seriesTitle} S{seasonNumber:D2}E{episodeNumber:D2}"
            : episodeTitle;
        builder.AppendLine($"  <title>{System.Security.SecurityElement.Escape(title)}</title>");
        builder.AppendLine($"  <showtitle>{System.Security.SecurityElement.Escape(seriesTitle)}</showtitle>");
        builder.AppendLine($"  <season>{seasonNumber}</season>");
        builder.AppendLine($"  <episode>{episodeNumber}</episode>");
        builder.AppendLine("</episodedetails>");
        return builder.ToString();
    }

    private static Guid CreateDeterministicGuid(string scope, string value, string? streamUrl)
    {
        using var md5 = MD5.Create();
        var raw = Encoding.UTF8.GetBytes(scope + "|" + value + "|" + streamUrl);
        var hash = md5.ComputeHash(raw);
        return new Guid(hash);
    }

    private sealed record ResolvedSettings(
        bool OverwriteExisting,
        bool CreateMetadataSidecars,
        bool CreateEpisodeNfo,
        string MetadataExtension);
}
