using Kafka.Ksql.Linq.Query.Builders.Functions;
using System;
using System.Linq.Expressions;

namespace Kafka.Ksql.Linq.Query.Builders.Common;

/// <summary>
/// Builder共通バリデーション
/// 設計理由：全Builderクラスで統一されたバリデーションロジック提供
/// </summary>
internal static class BuilderValidation
{
    /// <summary>
    /// 式木の基本バリデーション
    /// </summary>
    public static void ValidateExpression(Expression expression)
    {
        if (expression == null)
        {
            throw new ArgumentNullException(nameof(expression), "Expression cannot be null");
        }

        ValidateExpressionDepth(expression, maxDepth: 50);
        ValidateExpressionComplexity(expression);
    }

    /// <summary>
    /// 式木の深度チェック（スタックオーバーフロー防止）
    /// </summary>
    private static void ValidateExpressionDepth(Expression expression, int maxDepth, int currentDepth = 0)
    {
        if (currentDepth > maxDepth)
        {
            throw new InvalidOperationException($"Expression depth exceeds maximum allowed depth of {maxDepth}. " +
                "Consider simplifying the expression or breaking it into multiple operations.");
        }

        switch (expression)
        {
            case BinaryExpression binary:
                ValidateExpressionDepth(binary.Left, maxDepth, currentDepth + 1);
                ValidateExpressionDepth(binary.Right, maxDepth, currentDepth + 1);
                break;

            case UnaryExpression unary:
                ValidateExpressionDepth(unary.Operand, maxDepth, currentDepth + 1);
                break;

            case MethodCallExpression methodCall:
                if (methodCall.Object != null)
                    ValidateExpressionDepth(methodCall.Object, maxDepth, currentDepth + 1);

                foreach (var arg in methodCall.Arguments)
                    ValidateExpressionDepth(arg, maxDepth, currentDepth + 1);
                break;

            case LambdaExpression lambda:
                ValidateExpressionDepth(lambda.Body, maxDepth, currentDepth + 1);
                break;

            case NewExpression newExpr:
                foreach (var arg in newExpr.Arguments)
                    ValidateExpressionDepth(arg, maxDepth, currentDepth + 1);
                break;

            case ConditionalExpression conditional:
                ValidateExpressionDepth(conditional.Test, maxDepth, currentDepth + 1);
                ValidateExpressionDepth(conditional.IfTrue, maxDepth, currentDepth + 1);
                ValidateExpressionDepth(conditional.IfFalse, maxDepth, currentDepth + 1);
                break;
        }
    }

    /// <summary>
    /// 式木の複雑度チェック
    /// </summary>
    private static void ValidateExpressionComplexity(Expression expression)
    {
        var nodeCount = CountNodes(expression);
        const int maxNodes = 1000;

        if (nodeCount > maxNodes)
        {
            throw new InvalidOperationException($"Expression complexity exceeds maximum allowed nodes of {maxNodes}. " +
                $"Current expression has {nodeCount} nodes. " +
                "Consider simplifying the expression or breaking it into multiple operations.");
        }
    }

    /// <summary>
    /// 式木のノード数カウント
    /// </summary>
    private static int CountNodes(Expression expression)
    {
        var visitor = new NodeCountVisitor();
        visitor.Visit(expression);
        return visitor.NodeCount;
    }

    /// <summary>
    /// Lambda式からBody抽出（安全版）
    /// </summary>
    public static Expression? ExtractLambdaBody(Expression expression)
    {
        return expression switch
        {
            LambdaExpression lambda => lambda.Body,
            UnaryExpression { NodeType: ExpressionType.Quote, Operand: LambdaExpression lambda } => lambda.Body,
            _ => null
        };
    }

    /// <summary>
    /// MemberExpression抽出（安全版）
    /// </summary>
    public static MemberExpression? ExtractMemberExpression(Expression expression)
    {
        return expression switch
        {
            MemberExpression member => member,
            UnaryExpression unary when unary.Operand is MemberExpression member2 => member2,
            UnaryExpression unary => ExtractMemberExpression(unary.Operand),
            _ => null
        };
    }

    /// <summary>
    /// 引数数バリデーション
    /// </summary>
    public static void ValidateArgumentCount(string methodName, int actualCount, int expectedMin, int expectedMax = int.MaxValue)
    {
        if (actualCount < expectedMin || actualCount > expectedMax)
        {
            var expectedRange = expectedMax == int.MaxValue ? $"at least {expectedMin}" : $"{expectedMin}-{expectedMax}";
            throw new ArgumentException($"Method '{methodName}' expects {expectedRange} arguments, but got {actualCount}");
        }
    }

    /// <summary>
    /// NULL安全な文字列変換
    /// </summary>
    public static string SafeToString(object? value)
    {
        return value switch
        {
            null => "NULL",
            string str => $"'{str}'",
            bool b => b.ToString().ToLower(),
            _ => value.ToString() ?? "NULL"
        };
    }

    /// <summary>
    /// CASE式の THEN/ELSE 型一致を検証
    /// </summary>
    public static void ValidateConditionalTypes(Expression ifTrue, Expression ifFalse)
    {
        if (ifTrue.Type != ifFalse.Type)
        {
            throw new NotSupportedException($"CASE expression type mismatch: {ifTrue.Type} and {ifFalse.Type}");
        }
    }

    /// <summary>
    /// ネストした集約関数の使用禁止チェック
    /// </summary>
    public static void ValidateNoNestedAggregates(Expression expression)
    {
        var visitor = new NestedAggregateDetectionVisitor();
        visitor.Visit(expression);

        if (visitor.HasNestedAggregates)
        {
            throw new NotSupportedException("Nested aggregate functions are not supported");
        }
    }

    /// <summary>
    /// 集約関数のネスト検出用Visitor
    /// </summary>
    private class NestedAggregateDetectionVisitor : ExpressionVisitor
    {
        private int _aggregateDepth;
        public bool HasNestedAggregates { get; private set; }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            var methodName = node.Method.Name;

            if (KsqlFunctionRegistry.IsAggregateFunction(methodName))
            {
                if (_aggregateDepth > 0)
                {
                    HasNestedAggregates = true;
                    return node;
                }

                _aggregateDepth++;
                var result = base.VisitMethodCall(node);
                _aggregateDepth--;
                return result;
            }

            return base.VisitMethodCall(node);
        }
    }

    /// <summary>
    /// ノードカウンター用Visitor
    /// </summary>
    private class NodeCountVisitor : ExpressionVisitor
    {
        public int NodeCount { get; private set; }

        public override Expression? Visit(Expression? node)
        {
            if (node != null)
                NodeCount++;
            return base.Visit(node);
        }
    }
}
