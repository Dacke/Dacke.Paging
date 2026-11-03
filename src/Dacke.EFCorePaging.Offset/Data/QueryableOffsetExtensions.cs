using Dacke.Paging.Common.Enums;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace Dacke.EFCorePaging.Offset.Data;

public static class QueryableOffsetExtensions {
    private static readonly ConcurrentDictionary<(Type Type, string PropertyName), PropertyInfo?> PropertyCache = new();

    public static IQueryable<T> ApplySorting<T>(this IQueryable<T> query, string sortBy, SortDirection defaultDirection = SortDirection.Ascending, Dictionary<string, string>? propertyMap = null) {
        if (string.IsNullOrWhiteSpace(sortBy))
            return query;

        // Split multiple sort clauses (e.g. "LastName DESC, FirstName ASC")
        var sortFields = sortBy.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (sortFields.Length == 0)
            return query;

        var parameter = Expression.Parameter(typeof(T), "x");
        bool isFirst = true;

        foreach (var fieldSpec in sortFields) {
            // Split property name and optional direction token (e.g., "LastName" and "DESC")
            var parts = fieldSpec.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var propName = parts[0];
            var direction = defaultDirection;

            if (parts.Length > 1) {
                var dirToken = parts[1];
                if (dirToken.Equals("DESC", StringComparison.OrdinalIgnoreCase) || dirToken.Equals("1", StringComparison.OrdinalIgnoreCase)) {
                    direction = SortDirection.Descending;
                }
                else if (dirToken.Equals("ASC", StringComparison.OrdinalIgnoreCase) || dirToken.Equals("0", StringComparison.OrdinalIgnoreCase)) {
                    direction = SortDirection.Ascending;
                }
            }

            // Map DTO property name to Entity property name if mapping exists
            if (propertyMap != null && propertyMap.TryGetValue(propName, out var mappedName))
                propName = mappedName;

            var propInfo = GetCachedProperty(typeof(T), propName)
                ?? throw new ArgumentException($"Property '{propName}' not found on type {typeof(T).Name}");

            var propertyAccess = Expression.Property(parameter, propInfo);
            var lambda = Expression.Lambda(propertyAccess, parameter);

            // Determine LINQ extension method name
            string methodName = isFirst
                ? (direction == SortDirection.Ascending ? "OrderBy" : "OrderByDescending")
                : (direction == SortDirection.Ascending ? "ThenBy" : "ThenByDescending");

            var resultExpression = Expression.Call(
                typeof(Queryable),
                methodName,
                [typeof(T), propInfo.PropertyType],
                query.Expression,
                Expression.Quote(lambda));

            query = query.Provider.CreateQuery<T>(resultExpression);
            isFirst = false;
        }

        return query;
    }

    private static PropertyInfo? GetCachedProperty(Type type, string propertyName) {
        return PropertyCache.GetOrAdd((type, propertyName), key =>
            key.Type.GetProperty(key.PropertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance));
    }
}