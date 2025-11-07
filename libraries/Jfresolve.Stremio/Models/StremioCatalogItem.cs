namespace Jfresolve.Stremio.Models;

/// <summary>
/// Simplified representation of catalog items served by a Stremio add-on.
/// </summary>
/// <param name="Id">Item identifier.</param>
/// <param name="Type">Type such as movie or series.</param>
/// <param name="Name">Display name.</param>
/// <param name="Poster">Poster URL when provided.</param>
/// <param name="Background">Background image when provided.</param>
public sealed record StremioCatalogItem(
    string Id,
    string Type,
    string Name,
    string? Poster,
    string? Background);
