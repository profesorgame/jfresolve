namespace Jfresolve.Strm.Models;

/// <summary>
/// Result returned after generating STRM artifacts.
/// </summary>
/// <param name="RootDirectory">Directory that contains the generated artifacts.</param>
/// <param name="CreatedFiles">List of files that were created or overwritten.</param>
/// <param name="SkippedFiles">Files that already existed and were skipped.</param>
public sealed record StrmGenerationResult(
    string RootDirectory,
    IReadOnlyList<string> CreatedFiles,
    IReadOnlyList<string> SkippedFiles);
