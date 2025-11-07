namespace Jfresolve.Tmdb.Options;

/// <summary>
/// Options controlling how series metadata is expanded.
/// </summary>
public sealed class TmdbSeriesOptions
{
    /// <summary>
    /// Include season zero (specials) when downloading episodes.
    /// </summary>
    public bool IncludeSpecials { get; init; }

    /// <summary>
    /// Optional language override for season/episode lookups.
    /// </summary>
    public string? Language { get; init; }
}
