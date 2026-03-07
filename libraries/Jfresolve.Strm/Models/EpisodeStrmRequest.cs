namespace Jfresolve.Strm.Models;

/// <summary>
/// Input contract for a single episode STRM entry.
/// </summary>
/// <param name="SeasonNumber">Season number.</param>
/// <param name="EpisodeNumber">Episode number.</param>
/// <param name="StreamUrl">URL Jellyfin should load when playing the episode.</param>
/// <param name="Title">Optional episode title; used for NFO generation.</param>
/// <param name="ImdbId">Optional IMDb identifier.</param>
/// <param name="TmdbId">Optional TMDB identifier.</param>
/// <param name="LibraryItemId">Stable GUID representing the episode.</param>
/// <param name="Metadata">Additional metadata to serialize with the sidecar if enabled.</param>
public sealed record EpisodeStrmRequest(
    int SeasonNumber,
    int EpisodeNumber,
    string StreamUrl,
    string? Title = null,
    string? ImdbId = null,
    int? TmdbId = null,
    Guid? LibraryItemId = null,
    IReadOnlyDictionary<string, object?>? Metadata = null);
