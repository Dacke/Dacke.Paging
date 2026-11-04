using System.Linq.Expressions;
using System.Reflection;

namespace Dacke.Paging.Common.Extensions;

public static class ExpressionExtensions {
    public static PropertyInfo GetPropertyInfo<TModel, TProperty>(this Expression<Func<TModel, TProperty>> expression) {
        if (expression.Body is MemberExpression member)
            return (PropertyInfo)member.Member;

        if (expression.Body is UnaryExpression unary && unary.Operand is MemberExpression unaryMember)
            return (PropertyInfo)unaryMember.Member;

        throw new ArgumentException("Not a member access", nameof(expression));
    }
}
