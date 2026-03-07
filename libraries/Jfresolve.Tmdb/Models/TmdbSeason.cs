namespace Jfresolve.Tmdb.Models;

/// <summary>
/// Container for all episodes of a specific TMDB season.
/// </summary>
/// <param name="SeasonNumber">Sequential season number.</param>
/// <param name="Episodes">Episodes that belong to the season.</param>
public sealed record TmdbSeason(
    int SeasonNumber,
    IReadOnlyList<TmdbEpisode> Episodes);
