namespace Jfresolve.Tmdb.Options;

/// <summary>
/// Fine-grained filters for TMDB search calls.
/// </summary>
public sealed class TmdbSearchOptions
{
    /// <summary>
    /// Whether results flagged as adult should be included. Defaults to false.
    /// </summary>
    public bool IncludeAdult { get; init; }

    /// <summary>
    /// Whether unreleased titles are allowed. Defaults to true.
    /// </summary>
    public bool IncludeUnreleased { get; init; } = true;

    /// <summary>
    /// When <see cref="IncludeUnreleased"/> is false, how many days before release a title becomes visible.
    /// </summary>
    public int UnreleasedBufferDays { get; init; } = 0;

    /// <summary>
    /// Optional limit on the number of results to return.
    /// </summary>
    public int? MaxResults { get; init; }

    /// <summary>
    /// When true the client performs a lightweight external-ids lookup per item to populate IMDb IDs.
    /// </summary>
    public bool FetchExternalIds { get; init; }

    /// <summary>
    /// Optional year filter.
    /// </summary>
    public int? Year { get; init; }

    /// <summary>
    /// Optional language override.
    /// </summary>
    public string? Language { get; init; }

    /// <summary>
    /// Optional region override.
    /// </summary>
    public string? Region { get; init; }

    /// <summary>
    /// Creates a shallow clone with sensible defaults when caller passes null.
    /// </summary>
    public static TmdbSearchOptions Default => new();
}
