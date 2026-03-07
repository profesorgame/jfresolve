namespace Jfresolve.Stremio.Models;

/// <summary>
/// Represents the identity of an item for which streams should be fetched.
/// </summary>
/// <param name="Type">Stremio media type (movie, series, channel...).</param>
/// <param name="Id">Identifier known by the add-on.</param>
/// <param name="Season">Optional season index (series only).</param>
/// <param name="Episode">Optional episode index (series only).</param>
public sealed record StremioStreamRequest(
    string Type,
    string Id,
    int? Season = null,
    int? Episode = null);
