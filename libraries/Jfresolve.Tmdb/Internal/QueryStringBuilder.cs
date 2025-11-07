using System;
using System.Collections.Generic;
using System.Text;

namespace Jfresolve.Tmdb.Internal;

internal static class QueryStringBuilder
{
    public static string Build(string baseUrl, IReadOnlyDictionary<string, string?> parameters)
    {
        var builder = new StringBuilder();
        builder.Append(baseUrl);
        var hasQuery = baseUrl.Contains('?', StringComparison.Ordinal);
        foreach (var kvp in parameters)
        {
            if (string.IsNullOrEmpty(kvp.Value))
            {
                continue;
            }

            builder.Append(hasQuery ? '&' : '?');
            hasQuery = true;
            builder.Append(Uri.EscapeDataString(kvp.Key));
            builder.Append('=');
            builder.Append(Uri.EscapeDataString(kvp.Value));
        }

        return builder.ToString();
    }
}
