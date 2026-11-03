using Dapper;
using Dacke.Paging.Common.Enums;
using System.Text;

namespace Dacke.DapperPaging.Offset.Filtering; 

public static class DapperFilteringExtensions {
    /// <summary>
    /// Applies filtering from a PagedRequest containing a list of FilterRequests with Constraints and Operators 
    /// to generate a SQL WHERE clause and Dapper DynamicParameters.
    /// </summary>
    public static (string WhereClause, DynamicParameters Parameters) BuildSqlWhere(this PagedRequest request) {
        var parameters = new DynamicParameters();
        if (request?.Filters == null || request.Filters.Count == 0)
            return (string.Empty, parameters);

        var whereBuilder = new StringBuilder();
        var conditions = new List<string>();
        var paramIndex = 0;

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

                var paramName = $"@p_filter_{paramIndex++}";
                var condition = BuildCondition(constraint.PropertyName, constraint.MatchMode, paramName, stringValue, constraint.Value, parameters);

                if (string.IsNullOrEmpty(condition) == false)
                    constraintConditions.Add(condition);
            }

            if (constraintConditions.Count == 0)
                continue;

            var logicalOperator = filterRequest.Operator == FilterOperator.Any ? " OR " : " AND ";
            var combinedCondition = constraintConditions.Count == 1
                ? constraintConditions[0]
                : $"({string.Join(logicalOperator, constraintConditions)})";

            conditions.Add(combinedCondition);
        }

        if (conditions.Count > 0) {
            whereBuilder.Append("WHERE ");
            whereBuilder.Append(string.Join(globalFilterOperator, conditions));
        }

        return (whereBuilder.ToString(), parameters);
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