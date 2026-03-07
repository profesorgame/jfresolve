namespace Jfresolve.Tmdb.Models;

/// <summary>
/// Rich metadata for a single TMDB movie request.
/// </summary>
/// <param name="Metadata">Base metadata describing the movie.</param>
/// <param name="RuntimeMinutes">Runtime when available.</param>
/// <param name="Tagline">Movie tagline text.</param>
public sealed record TmdbMovieDetails(
    TmdbMetadata Metadata,
    int? RuntimeMinutes,
    string? Tagline);
