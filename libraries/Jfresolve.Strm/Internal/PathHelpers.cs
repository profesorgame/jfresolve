using System.IO;
using System.IO.Abstractions;
using System.Text;
using System.Text.RegularExpressions;

namespace Jfresolve.Strm.Internal;

internal static class PathHelpers
{
    private static readonly Regex InvalidChars = new("[\\/:*?\"<>|]+", RegexOptions.Compiled);

    public static string SanitizeName(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return "Unknown";
        }

        var sanitized = InvalidChars.Replace(input, " ");
        sanitized = sanitized.Trim();
        return string.Join(" ", sanitized.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    public static void EnsureDirectory(IFileSystem fileSystem, string path)
    {
        if (!fileSystem.Directory.Exists(path))
        {
            fileSystem.Directory.CreateDirectory(path);
        }
    }

    public static string CombineWithSanitizedName(string root, params string[] parts)
    {
        var builder = new StringBuilder(root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        foreach (var part in parts)
        {
            builder.Append(Path.DirectorySeparatorChar);
            builder.Append(SanitizeName(part));
        }

        return builder.ToString();
    }
}
