using System;

namespace Jfresolve.Stremio.Options;

/// <summary>
/// Options for connecting to a Stremio add-on.
/// </summary>
public sealed class StremioAddonOptions
{
    /// <summary>
    /// Base URL or manifest URL of the add-on.
    /// Examples: https://example.com/manifest.json or stremio://example.com.
    /// </summary>
    public string ManifestUrl { get; init; } = string.Empty;

    /// <summary>
    /// Timeout applied to HTTP requests. Defaults to 10 seconds when null.
    /// </summary>
    public TimeSpan? RequestTimeout { get; init; }
}
