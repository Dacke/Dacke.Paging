using Dacke.Paging.Common.Cursor;
using Dacke.Paging.Common.Enums;
using Dacke.Paging.Common.Interfaces;

namespace Dacke.DapperPaging.Cursor.Sorting;

public static class DapperCursorSortingExtensions {
    public static string BuildCursorSqlSorting(this ISortableRequest request, PagingContext pagingContext, PagingMetadata metadata, Dictionary<string, string>? propertyMap = null) {
        var clauses = string.IsNullOrWhiteSpace(request.SortBy) == false
            ? request.SortBy.Split(',', StringSplitOptions.RemoveEmptyEntries)
            : [metadata.DefaultSortColumn];

        var translatedClauses = new List<string>();
        var seenProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        string defaultDirectionText = request.DefaultSortDirection == SortDirection.Descending ? "DESC" : "ASC";

        foreach (var clause in clauses) {
            var parts = clause.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var propName = parts[0];
            seenProperties.Add(propName);

            string direction = parts.Length > 1 ? parts[1].ToUpperInvariant() : defaultDirectionText;

            // Invert direction when navigating backward
            if (pagingContext.NavigationDirection == NavigationDirection.Backward) {
                direction = direction == "ASC" ? "DESC" : "ASC";
            }

            if (propertyMap != null && propertyMap.TryGetValue(propName, out var mappedName)) {
                propName = mappedName;
            }

            translatedClauses.Add($"{propName} {direction}");
        }

        // Guarantee primary key tie-breaker for deterministic cursor navigation
        if (seenProperties.Contains(metadata.EntityKeyColumn) == false) {
            var pkCol = propertyMap != null && propertyMap.TryGetValue(metadata.EntityKeyColumn, out var mappedPk)
                ? mappedPk
                : metadata.EntityKeyColumn;

            string pkDirection = (pagingContext.NavigationDirection == NavigationDirection.Backward) ? (defaultDirectionText == "ASC" ? "DESC" : "ASC") : defaultDirectionText;
            translatedClauses.Add($"{pkCol} {pkDirection}");
        }

        return $"ORDER BY {string.Join(", ", translatedClauses)}";
    }
}