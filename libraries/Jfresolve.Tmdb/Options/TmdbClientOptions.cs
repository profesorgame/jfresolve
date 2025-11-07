namespace Jfresolve.Tmdb.Options;

/// <summary>
/// Required configuration for accessing TMDB.
/// </summary>
public sealed class TmdbClientOptions
{
    /// <summary>
    /// TMDB API key. Required for all requests.
    /// </summary>
    public string ApiKey { get; init; } = string.Empty;

    /// <summary>
    /// Base URL for TMDB REST API. Defaults to https://api.themoviedb.org/3.
    /// </summary>
    public string BaseUrl { get; init; } = "https://api.themoviedb.org/3";

    /// <summary>
    /// Base URL for TMDB images. Defaults to https://image.tmdb.org/t/p.
    /// </summary>
    public string ImageBaseUrl { get; init; } = "https://image.tmdb.org/t/p";

    /// <summary>
    /// Poster size suffix appended to poster paths. Defaults to w342.
    /// </summary>
    public string PosterSize { get; init; } = "w342";

    /// <summary>
    /// Backdrop size suffix appended to backdrop paths. Defaults to w1280.
    /// </summary>
    public string BackdropSize { get; init; } = "w1280";

    /// <summary>
    /// Language code to request (ISO 639-1). Defaults to en-US.
    /// </summary>
    public string Language { get; init; } = "en-US";

    /// <summary>
    /// Region code (ISO 3166-1 alpha-2). Optional.
    /// </summary>
    public string? Region { get; init; }
}
