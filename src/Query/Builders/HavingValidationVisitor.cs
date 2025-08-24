using Kafka.Ksql.Linq.Query.Builders.Functions;
using System.Linq.Expressions;

namespace Kafka.Ksql.Linq.Query.Builders;

/// <summary>
/// HAVING句バリデーション用Visitor
/// </summary>
internal class HavingValidationVisitor : ExpressionVisitor
{
    public bool HasInvalidReferences { get; private set; }
    private bool _insideAggregateFunction;

    protected override Expression VisitMember(MemberExpression node)
    {
        // 集約関数内でないメンバーアクセスは、GROUP BYカラムである必要がある
        // この実装では簡略化（実際にはGROUP BYカラムリストとの照合が必要）
        if (!_insideAggregateFunction && node.Expression is ParameterExpression)
        {
            // ここで実際のGROUP BYカラムとの照合を行う（実装簡略化）
            // 実際の実装では、GROUP BYで使用されたカラムのリストと照合
        }

        return base.VisitMember(node);
    }


    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        var wasInside = _insideAggregateFunction;
        if (KsqlFunctionRegistry.IsAggregateFunction(node.Method.Name))
        {
            _insideAggregateFunction = true;
        }

        var result = base.VisitMethodCall(node);
        _insideAggregateFunction = wasInside;
        return result;
    }
}
