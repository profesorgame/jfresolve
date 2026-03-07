using System.Collections.Generic;

namespace Jfresolve.Stremio.Models;

/// <summary>
/// Minimal manifest information returned by a Stremio add-on.
/// </summary>
/// <param name="Id">Manifest identifier.</param>
/// <param name="Name">Display name of the add-on.</param>
/// <param name="Version">Manifest version.</param>
/// <param name="Description">Optional description.</param>
/// <param name="Resources">Resource endpoints advertised by the add-on.</param>
/// <param name="Types">Supported item types.</param>
public sealed record StremioManifest(
    string Id,
    string Name,
    string Version,
    string? Description,
    IReadOnlyList<string> Resources,
    IReadOnlyList<string> Types);
