namespace Jfresolve.Strm.Options;

/// <summary>
/// Controls how STRM files and sidecar metadata are produced.
/// </summary>
public sealed class StrmGenerationSettings
{
    /// <summary>
    /// When true existing STRM files are overwritten instead of skipped. Null falls back to defaults.
    /// </summary>
    public bool? OverwriteExisting { get; init; }

    /// <summary>
    /// Whether metadata sidecar files should be produced alongside STRM files. Null falls back to defaults.
    /// </summary>
    public bool? CreateMetadataSidecars { get; init; }

    /// <summary>
    /// Whether NFO files for episodes should be generated. Null falls back to defaults.
    /// </summary>
    public bool? CreateEpisodeNfo { get; init; }

    /// <summary>
    /// File extension for generated metadata sidecars.
    /// </summary>
    public string? MetadataExtension { get; init; }
}
