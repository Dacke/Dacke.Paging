using Paging.Common.Enums;

namespace DapperPaging.Offset;

public static class DapperSortingExtensions {
    /// <summary>
    /// Builds a SQL ORDER BY clause from sorting parameters, respecting individual column directions 
    /// and falling back to the default sort direction when no direction is specified.
    /// </summary>
    public static string BuildSqlSorting(this PagedRequest request, Dictionary<string, string>? propertyMap = null) {
        if (string.IsNullOrWhiteSpace(request.SortBy) == true)
            return string.Empty;

        var clauses = request.SortBy.Split(',', StringSplitOptions.RemoveEmptyEntries);
        var translatedClauses = new List<string>();

        string defaultDirectionText = request.DefaultSortDirection == SortDirection.Descending ? "DESC" : "ASC";

        foreach (var clause in clauses) {
            var parts = clause.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var propName = parts[0];

            string direction;
            if (parts.Length > 1 == true)
                direction = parts[1].ToUpperInvariant();
            else
                direction = defaultDirectionText;

            // Map DTO property names to database column names if a map is provided
            if (propertyMap != null && propertyMap.TryGetValue(propName, out var mappedName) == true)
                propName = mappedName;

            translatedClauses.Add($"{propName} {direction}");
        }

        return $"ORDER BY {string.Join(", ", translatedClauses)}";
    }
}