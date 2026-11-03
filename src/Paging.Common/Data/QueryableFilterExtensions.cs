using Paging.Common.Enums;
using Paging.Common.Filtering;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;

namespace Paging.Common.Data;

public static class QueryableFilterExtensions {
    private static readonly ConcurrentDictionary<(Type Type, string PropertyName), PropertyInfo?> PropertyCache = new();

    private static readonly MethodInfo StartsWithMethod = typeof(string).GetMethod(nameof(string.StartsWith), [typeof(string)])!;
    private static readonly MethodInfo EndsWithMethod = typeof(string).GetMethod(nameof(string.EndsWith), [typeof(string)])!;
    private static readonly MethodInfo ContainsMethod = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;

    /// <summary>
    /// Applies dynamic filter criteria to an IQueryable collection based on structured filter requests.
    /// </summary>
    /// <remarks>
    /// This extension method enables flexible, dynamic filtering of queryable collections by building
    /// expression trees at runtime. It supports multiple filter constraints per field with configurable
    /// logical operators (AND/OR) at both field and global levels. Filters are combined using either
    /// AND (All) or OR (Any) logic as specified by the globalOperator parameter.
    /// </remarks>
    /// <typeparam name="T">The type of elements in the queryable collection.</typeparam>
    /// <param name="query">The IQueryable collection to filter.</param>
    /// <param name="filters">A list of field-level filter requests, each containing multiple constraints. Null or empty lists return the unfiltered query.</param>
    /// <param name="globalOperator">The logical operator to apply when combining field-level predicates. Defaults to FilterOperator.All (AND logic).</param>
    /// <returns>An IQueryable collection with the compiled filter predicate applied, or the original query if no valid filters are provided.</returns>
    public static IQueryable<T> ApplyFilters<T>(this IQueryable<T> query, List<FilterRequest>? filters, FilterOperator globalOperator = FilterOperator.All) {
        if (filters == null || filters.Count == 0)
            return query;

        var parameter = Expression.Parameter(typeof(T), "x");
        Expression? rootPredicate = null;

        foreach (var fieldFilter in filters) {
            if (fieldFilter.Constraints == null || fieldFilter.Constraints.Count == 0)
                continue;

            Expression? fieldPredicate = null;
            bool isFieldAnd = fieldFilter.Operator == FilterOperator.All;

            foreach (var rule in fieldFilter.Constraints) {
                if (string.IsNullOrWhiteSpace(rule.PropertyName) || string.IsNullOrWhiteSpace(rule.Value))
                    continue;

                var ruleExpr = BuildRuleExpression<T>(parameter, rule);
                if (ruleExpr == null)
                    continue;

                fieldPredicate = fieldPredicate == null
                    ? ruleExpr
                    : (isFieldAnd ? Expression.AndAlso(fieldPredicate, ruleExpr) : Expression.OrElse(fieldPredicate, ruleExpr));
            }

            if (fieldPredicate == null)
                continue;

            bool isGlobalAnd = globalOperator == FilterOperator.All;
            rootPredicate = rootPredicate == null
                ? fieldPredicate
                : (isGlobalAnd ? Expression.AndAlso(rootPredicate, fieldPredicate) : Expression.OrElse(rootPredicate, fieldPredicate));
        }

        if (rootPredicate == null)
            return query;

        var lambda = Expression.Lambda<Func<T, bool>>(rootPredicate, parameter);
        return query.Where(lambda);
    }

    /// <summary>
    /// Builds a filter expression for a single rule by creating an appropriate comparison or string matching operation.
    /// </summary>
    /// <remarks>
    /// Constructs an expression tree node that represents a single filter constraint. The method handles type conversion
    /// for common types (Guid, DateTime, DateTimeOffset, Enum) and supports multiple match modes including equality,
    /// comparison operators, and string operations (StartsWith, EndsWith, Contains). Returns null if property resolution
    /// fails or type conversion cannot be performed.
    /// </remarks>
    /// <typeparam name="T">The type of the entity being filtered.</typeparam>
    /// <param name="parameter">The parameter expression representing the entity instance.</param>
    /// <param name="rule">The filter constraint containing the property name, value, and match mode.</param>
    /// <returns>An expression representing the filter condition, or null if the property cannot be resolved or the value cannot be converted.</returns>
    private static Expression? BuildRuleExpression<T>(ParameterExpression parameter, ColumnFilter rule) {
        var propInfo = GetCachedProperty(typeof(T), rule.PropertyName);
        if (propInfo == null)
            return null;

        var propertyAccess = Expression.Property(parameter, propInfo);
        var targetType = Nullable.GetUnderlyingType(propInfo.PropertyType) ?? propInfo.PropertyType;

        object? convertedVal;
        try {
            convertedVal = targetType switch {
                var t when t == typeof(Guid) => Guid.Parse(rule.Value),
                var t when t == typeof(DateTime) => DateTime.Parse(rule.Value),
                var t when t == typeof(DateTimeOffset) => DateTimeOffset.Parse(rule.Value),
                var t when t.IsEnum => Enum.Parse(t, rule.Value, ignoreCase: true),
                _ => TypeDescriptor.GetConverter(targetType).ConvertFromInvariantString(rule.Value)
            };
        }
        catch {
            return null;
        }

        var constant = Expression.Constant(convertedVal, propInfo.PropertyType);

        if (targetType == typeof(string)) {
            return rule.MatchMode switch {
                MatchMode.StartsWith => Expression.Call(propertyAccess, StartsWithMethod, constant),
                MatchMode.EndsWith => Expression.Call(propertyAccess, EndsWithMethod, constant),
                MatchMode.Contains => Expression.Call(propertyAccess, ContainsMethod, constant),
                MatchMode.NotContains => Expression.Not(Expression.Call(propertyAccess, ContainsMethod, constant)),
                MatchMode.Equals => Expression.Equal(propertyAccess, constant),
                MatchMode.NotEquals => Expression.NotEqual(propertyAccess, constant),
                _ => Expression.Equal(propertyAccess, constant)
            };
        }

        return rule.MatchMode switch {
            MatchMode.Equals => Expression.Equal(propertyAccess, constant),
            MatchMode.NotEquals => Expression.NotEqual(propertyAccess, constant),
            MatchMode.GreaterThan => Expression.GreaterThan(propertyAccess, constant),
            MatchMode.GreaterThanOrEqualTo => Expression.GreaterThanOrEqual(propertyAccess, constant),
            MatchMode.LessThan => Expression.LessThan(propertyAccess, constant),
            MatchMode.LessThanOrEqualTo => Expression.LessThanOrEqual(propertyAccess, constant),
            _ => Expression.Equal(propertyAccess, constant)
        };
    }

    /// <summary>
    /// Retrieves cached property metadata for efficient repeated lookups.
    /// </summary>
    /// <remarks>
    /// Uses a thread-safe ConcurrentDictionary to cache PropertyInfo objects by type and property name,
    /// reducing reflection overhead when the same property is accessed multiple times.
    /// </remarks>
    /// <param name="type">The type containing the property.</param>
    /// <param name="propertyName">The name of the property to retrieve (case-insensitive).</param>
    /// <returns>The PropertyInfo for the requested property, or null if the property does not exist.</returns>
    private static PropertyInfo? GetCachedProperty(Type type, string propertyName) {
        return PropertyCache.GetOrAdd((type, propertyName), key =>
            key.Type.GetProperty(key.PropertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance));
    }
}