namespace Jfresolve.Tmdb.Models;

/// <summary>
/// Detailed metadata for a single episode within a TMDB season response.
/// </summary>
/// <param name="SeasonNumber">Season number as reported by TMDB.</param>
/// <param name="EpisodeNumber">Episode number inside the season.</param>
/// <param name="Title">Episode title.</param>
/// <param name="Overview">Short plot overview.</param>
/// <param name="AirDate">Premier air date in UTC when available.</param>
/// <param name="ImdbId">IMDb identifier when available.</param>
/// <param name="StillPath">Relative still image path.</param>
public sealed record TmdbEpisode(
    int SeasonNumber,
    int EpisodeNumber,
    string Title,
    string? Overview,
    DateTimeOffset? AirDate,
    string? ImdbId,
    string? StillPath);
