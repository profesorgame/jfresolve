namespace Jfresolve.Tmdb.Models;

/// <summary>
/// Aggregated representation for a TMDB TV series including season breakdown.
/// </summary>
/// <param name="Metadata">High level metadata for the series itself.</param>
/// <param name="Seasons">Ordered seasons with their respective episodes.</param>
public sealed record TmdbSeriesDetails(
    TmdbMetadata Metadata,
    IReadOnlyList<TmdbSeason> Seasons);
