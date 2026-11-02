using EFCorePaging.Common.Enums;
using EFCorePaging.Common.Filtering;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;

namespace EFCorePaging.Common.Data;

public static class QueryableFilterExtensions {
    private static readonly ConcurrentDictionary<(Type Type, string PropertyName), PropertyInfo?> PropertyCache = new();

    private static readonly MethodInfo StartsWithMethod = typeof(string).GetMethod(nameof(string.StartsWith), [typeof(string)])!;
    private static readonly MethodInfo EndsWithMethod = typeof(string).GetMethod(nameof(string.EndsWith), [typeof(string)])!;
    private static readonly MethodInfo ContainsMethod = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;

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

    private static PropertyInfo? GetCachedProperty(Type type, string propertyName) {
        return PropertyCache.GetOrAdd((type, propertyName), key =>
            key.Type.GetProperty(key.PropertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance));
    }
}