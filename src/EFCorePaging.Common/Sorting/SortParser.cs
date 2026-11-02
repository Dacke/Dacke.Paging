using EFCorePaging.Common.Enums;

namespace EFCorePaging.Common.Sorting;

public static class SortParser {
    public static List<(string PropertyName, SortDirection Direction)> ParseSortColumns(string? sortBy, SortDirection defaultDirection = SortDirection.Ascending, string defaultColumn = "LastName") {
        var result = new List<(string PropertyName, SortDirection Direction)>();

        if (string.IsNullOrWhiteSpace(sortBy)) {
            result.Add((defaultColumn, defaultDirection));
            return result;
        }

        // Split by comma for multi-column sort e.g., "LastName ASC, FirstName DESC"
        var segments = sortBy.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var segment in segments) {
            var parts = segment.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            string propName = parts[0];

            SortDirection direction = parts.Length > 1
                ? parts[1].ToUpperInvariant() switch {
                    "ASC" or "ASCENDING" => SortDirection.Ascending,
                    "DESC" or "DESCENDING" => SortDirection.Descending,
                    _ => defaultDirection
                }
                : defaultDirection;

            result.Add((propName, direction));
        }

        return result;
    }
}