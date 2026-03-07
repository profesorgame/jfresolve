namespace Jfresolve.Strm.Models;

/// <summary>
/// Input contract for creating STRM files for a TV series.
/// </summary>
/// <param name="Title">Series title.</param>
/// <param name="DestinationDirectory">Root folder where season folders will be created.</param>
/// <param name="Year">Premiere year.</param>
/// <param name="ImdbId">Optional IMDb identifier for the series.</param>
/// <param name="TmdbId">Optional TMDB identifier for the series.</param>
/// <param name="ProviderIds">Additional provider IDs.</param>
/// <param name="LibraryItemId">Stable GUID representing the series.</param>
/// <param name="Seasons">Season payloads.</param>
public sealed record SeriesStrmRequest(
    string Title,
    string DestinationDirectory,
    int? Year,
    string? ImdbId,
    int? TmdbId,
    IReadOnlyDictionary<string, string>? ProviderIds,
    Guid? LibraryItemId,
    IReadOnlyList<SeasonStrmRequest> Seasons);
