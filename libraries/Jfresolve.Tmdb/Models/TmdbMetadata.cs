namespace Jfresolve.Tmdb.Models;

/// <summary>
/// Lightweight representation of a TMDB search result enriched with key identifiers.
/// </summary>
/// <param name="TmdbId">Numeric TMDB identifier.</param>
/// <param name="Title">Canonical title from TMDB.</param>
/// <param name="Kind">Media type (movie or series).</param>
/// <param name="Overview">Plot summary when available.</param>
/// <param name="Year">Release year derived from TMDB dates.</param>
/// <param name="PosterPath">Relative poster path from TMDB.</param>
/// <param name="BackdropPath">Relative backdrop path from TMDB.</param>
/// <param name="Popularity">Popularity score reported by TMDB.</param>
/// <param name="IsAdult">Whether the title is flagged as adult content.</param>
/// <param name="IsAnime">True when the TMDB genre list indicates animation/anime.</param>
/// <param name="ExternalIdentifiers">External IDs keyed by provider (e.g. "imdb").</param>
public sealed record TmdbMetadata(
    int TmdbId,
    string Title,
    MediaKind Kind,
    string? Overview,
    int? Year,
    string? PosterPath,
    string? BackdropPath,
    double Popularity,
    bool IsAdult,
    bool IsAnime,
    IReadOnlyDictionary<string, string> ExternalIdentifiers);
