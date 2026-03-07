namespace Jfresolve.Strm.Models;

/// <summary>
/// Input contract for creating a STRM entry for a movie.
/// </summary>
/// <param name="Title">Display title used for folder/metadata generation.</param>
/// <param name="StreamUrl">The remote URL or plugin endpoint Jellyfin should stream.</param>
/// <param name="DestinationDirectory">Directory where the STRM should be written. The generator creates it when missing.</param>
/// <param name="Year">Release year for metadata purposes.</param>
/// <param name="ImdbId">Optional IMDb identifier.</param>
/// <param name="TmdbId">Optional TMDB identifier.</param>
/// <param name="ProviderIds">Additional provider identifiers to persist in the sidecar file.</param>
/// <param name="LibraryItemId">Stable GUID representing the movie inside the consuming library.</param>
/// <param name="Metadata">Optional free-form metadata that will be serialized next to the STRM file.</param>
public sealed record MovieStrmRequest(
    string Title,
    string StreamUrl,
    string DestinationDirectory,
    int? Year = null,
    string? ImdbId = null,
    int? TmdbId = null,
    IReadOnlyDictionary<string, string>? ProviderIds = null,
    Guid? LibraryItemId = null,
    IReadOnlyDictionary<string, object?>? Metadata = null);
