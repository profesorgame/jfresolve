using System;
using System.Globalization;

namespace Jfresolve.Tmdb.Internal;

internal static class DateParser
{
    public static int? YearFromDate(string? date)
    {
        if (string.IsNullOrWhiteSpace(date))
        {
            return null;
        }

        if (DateTime.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dt))
        {
            return dt.Year;
        }

        return null;
    }

    public static DateTimeOffset? ParseDate(string? date)
    {
        if (string.IsNullOrWhiteSpace(date))
        {
            return null;
        }

        if (DateTimeOffset.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dto))
        {
            return dto;
        }

        if (DateTime.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dt))
        {
            return new DateTimeOffset(dt, TimeSpan.Zero);
        }

        return null;
    }
}
