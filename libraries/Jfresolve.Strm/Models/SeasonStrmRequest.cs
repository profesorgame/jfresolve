namespace Jfresolve.Strm.Models;

/// <summary>
/// Bundles all episode requests for a given season.
/// </summary>
/// <param name="SeasonNumber">Season number.</param>
/// <param name="Episodes">Episodes to materialize inside the season folder.</param>
public sealed record SeasonStrmRequest(
    int SeasonNumber,
    IReadOnlyList<EpisodeStrmRequest> Episodes);
