using System.Collections.Generic;

namespace Jfresolve.Stremio.Models;

/// <summary>
/// Represents a playable stream entry returned by a Stremio add-on.
/// </summary>
/// <param name="Url">Primary stream URL or redirect target.</param>
/// <param name="Title">Optional human readable title.</param>
/// <param name="Description">Optional description.</param>
/// <param name="BehaviorHints">Optional additional hints returned by the add-on.</param>
public sealed record StremioStream(
    string Url,
    string? Title,
    string? Description,
    IReadOnlyDictionary<string, object?>? BehaviorHints);
