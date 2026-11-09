using Dapper;
using Dacke.Paging.Common.Enums;
using Dacke.Paging.Common.Interfaces;
using Dacke.Paging.Common.Cursor;

namespace Dacke.DapperPaging.Cursor.Filtering;

public static class DapperCursorFilteringExtensions {
    /// <summary>
    /// Applies filtering and keyset cursor boundary conditions from a PagedRequest and PagingContext 
    /// to generate a single combined SQL WHERE clause and Dapper DynamicParameters.
    /// </summary>
    public static (string WhereClause, DynamicParameters Parameters) BuildSqlWhere(this IFilterableRequest request, PagingContext pagingContext, Dictionary<string, string> propertyMap) {
        var parameters = new DynamicParameters();
        var filterConditions = new List<string>();
        var paramIndex = 0;

        ArgumentNullException.ThrowIfNull(request);

        // Build standard property filters
        if (request.Filters != null && request.Filters.Count > 0) {
            var globalFilterOperator = request.GlobalFilterOperator == FilterOperator.Any ? " OR " : " AND ";

            foreach (var filterRequest in request.Filters) {
                if (filterRequest == null || filterRequest.Constraints == null || filterRequest.Constraints.Count == 0)
                    continue;

                var constraintConditions = new List<string>();
                foreach (var constraint in filterRequest.Constraints) {
                    if (constraint == null || constraint.Value == null)
                        continue;

                    var stringValue = constraint.Value.ToString() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(stringValue) == true && constraint.MatchMode != MatchMode.Equals)
                        continue;

                    // Resolve mapping from propertyMap so "Id" correctly translates to "PersonId"
                    var propName = constraint.PropertyName;
                    if (propertyMap != null && propertyMap.TryGetValue(propName, out var mappedName))
                        propName = mappedName;

                    var paramName = $"@p_filter_{paramIndex++}";
                    var condition = BuildCondition(propName, constraint.MatchMode, paramName, stringValue, constraint.Value, parameters);

                    if (string.IsNullOrEmpty(condition) == false)
                        constraintConditions.Add(condition);
                }

                if (constraintConditions.Count == 0)
                    continue;

                var logicalOperator = filterRequest.Operator == FilterOperator.Any ? " OR " : " AND ";
                var combinedCondition = constraintConditions.Count == 1
                    ? constraintConditions[0]
                    : $"({string.Join(logicalOperator, constraintConditions)})";

                filterConditions.Add(combinedCondition);
            }
        }

        var filterWhere = filterConditions.Count > 0
            ? string.Join(request.GlobalFilterOperator == FilterOperator.Any ? " OR " : " AND ", filterConditions)
            : string.Empty;

        // Build keyset cursor WHERE clause from pagingContext if a cursor is present
        string cursorWhereClause = string.Empty;
        if (pagingContext?.Cursor != null) {
            bool isBackward = pagingContext.NavigationDirection == NavigationDirection.Backward;
            var cursorClauses = new List<string>();

            var sortColumns = pagingContext.SortColumns?.ToList() ?? [];
            var cursorValues = pagingContext.Cursor.SortValues?.ToList() ?? [];

            var allColumns = new List<(string PropertyName, SortDirection Direction, object Value)>();
            for (int columnIndex = 0; columnIndex < sortColumns.Count; columnIndex++) {
                var col = sortColumns[columnIndex];
                if (columnIndex < cursorValues.Count)
                    allColumns.Add(new(col.PropertyName, col.Direction, cursorValues[columnIndex].Value!));
            }

            // Explicitly include Primary Key tie-breaker from the cursor
            if (pagingContext.Cursor.PrimaryKeyValue != null) {
                var pkPropName = pagingContext.PrimaryKeyColumn;
                // Parse string cursor value to native Guid so Dapper maps it as UNIQUEIDENTIFIER
                object pkValue = pagingContext.Cursor.PrimaryKeyValue;
                if (pkValue is string strGuid && Guid.TryParse(strGuid, out var parsedGuid))
                    pkValue = parsedGuid;

                allColumns.Add((pkPropName, SortDirection.Ascending, pkValue));
            }


            // Pre-build parameter names and add parameters
            var parameterMap = new List<string>();
            for (int columnIndex = 0; columnIndex < allColumns.Count; columnIndex++) {
                var (_, _, val) = allColumns[columnIndex];
                var paramName = $"@p_cursor_val_{paramIndex++}";
                parameters.Add(paramName, val);
                parameterMap.Add(paramName);
            }

            for (int columnIndex = 0; columnIndex < allColumns.Count; columnIndex++) {
                var (propName, direction, _) = allColumns[columnIndex];
                var dbColumnName = propertyMap.TryGetValue(propName, out var mapped)
                    ? mapped
                    : propName;

                var safeCol = $"[{dbColumnName.Replace(";", "").Replace("'", "")}]";

                bool descending = direction == SortDirection.Descending;
                string op = isBackward ? descending ? ">" : "<" : descending ? "<" : ">";

                var subConditions = new List<string>();
                // Equality checks for all preceding columns
                for (int idx = 0; idx < columnIndex; idx++) {
                    var (prevProp, _, _) = allColumns[idx];
                    var prevDbCol = propertyMap.TryGetValue(prevProp, out var prevMapped)
                        ? prevMapped
                        : prevProp;
                    var safePrevCol = $"[{prevDbCol.Replace(";", "").Replace("'", "")}]";

                    var paramName = parameterMap[idx];
                    subConditions.Add($"{safePrevCol} = {paramName}");
                }

                // Inequality check for the current column
                var currParamName = parameterMap[columnIndex];
                subConditions.Add($"{safeCol} {op} {currParamName}");

                if (subConditions.Count == 1)
                    cursorClauses.Add(subConditions[0]);
                else if (subConditions.Count > 1)
                    cursorClauses.Add($"({string.Join(" AND ", subConditions)})");
            }

            if (cursorClauses.Count > 0)
                cursorWhereClause = string.Join(" OR ", cursorClauses);
        }

        var combinedWhere = (string.IsNullOrEmpty(filterWhere) == false, string.IsNullOrEmpty(cursorWhereClause) == false) switch {
            (true, true) => $"WHERE {filterWhere} AND ({cursorWhereClause})",
            (true, false) => $"WHERE {filterWhere}",
            (false, true) => $"WHERE {cursorWhereClause}",
            _ => string.Empty
        };

        return (combinedWhere, parameters);
    }

    private static string BuildCondition(string columnName, MatchMode matchMode, string paramName, string stringValue, object rawValue, DynamicParameters parameters) {
        var safeColumn = $"[{columnName.Replace(";", "").Replace("'", "")}]";

        switch (matchMode) {
            case MatchMode.Equals:
                parameters.Add(paramName, rawValue);
                return $"{safeColumn} = {paramName}";

            case MatchMode.NotEquals:
                parameters.Add(paramName, rawValue);
                return $"{safeColumn} != {paramName}";

            case MatchMode.Contains:
                parameters.Add(paramName, $"%{stringValue}%");
                return $"{safeColumn} LIKE {paramName}";

            case MatchMode.NotContains:
                parameters.Add(paramName, $"%{stringValue}%");
                return $"{safeColumn} NOT LIKE {paramName}";

            case MatchMode.StartsWith:
                parameters.Add(paramName, $"{stringValue}%");
                return $"{safeColumn} LIKE {paramName}";

            case MatchMode.EndsWith:
                parameters.Add(paramName, $"%{stringValue}");
                return $"{safeColumn} LIKE {paramName}";

            case MatchMode.LessThan:
                parameters.Add(paramName, rawValue);
                return $"{safeColumn} < {paramName}";

            case MatchMode.LessThanOrEqualTo:
                parameters.Add(paramName, rawValue);
                return $"{safeColumn} <= {paramName}";

            case MatchMode.GreaterThan:
                parameters.Add(paramName, rawValue);
                return $"{safeColumn} > {paramName}";

            case MatchMode.GreaterThanOrEqualTo:
                parameters.Add(paramName, rawValue);
                return $"{safeColumn} >= {paramName}";

            default:
                parameters.Add(paramName, rawValue);
                return $"{safeColumn} = {paramName}";
        }
    }
}