using System;
using System.Linq;
using System.Linq.Expressions;

namespace Kafka.Ksql.Linq.Query.Builders;
internal class OrderByComplexityVisitor : ExpressionVisitor
{
    public bool HasComplexExpressions { get; private set; }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        // 二項演算は複雑な式とみなす
        HasComplexExpressions = true;
        return base.VisitBinary(node);
    }

    protected override Expression VisitConditional(ConditionalExpression node)
    {
        // 条件式は複雑な式とみなす
        HasComplexExpressions = true;
        return base.VisitConditional(node);
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        var methodName = node.Method.Name;

        // 許可された関数以外は複雑とみなす
        var allowedMethods = new[]
        {
            "OrderBy", "OrderByDescending", "ThenBy", "ThenByDescending",
            "RowTime",
            "ToUpper", "ToLower", "Abs", "Year", "Month", "Day"
        };

        if (!allowedMethods.Contains(methodName))
        {
            HasComplexExpressions = true;
        }

        return base.VisitMethodCall(node);
    }
}
