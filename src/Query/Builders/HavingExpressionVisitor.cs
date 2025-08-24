using Kafka.Ksql.Linq.Query.Builders.Common;
using Kafka.Ksql.Linq.Query.Builders.Functions;
using System;
using System.Linq.Expressions;

namespace Kafka.Ksql.Linq.Query.Builders;

/// <summary>
/// HAVING句専用ExpressionVisitor
/// </summary>
internal class HavingExpressionVisitor : ExpressionVisitor
{
    private string _result = string.Empty;

    public string GetResult()
    {
        return _result;
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        var left = ProcessExpression(node.Left);
        var right = ProcessExpression(node.Right);
        var varoperator = GetSqlOperator(node.NodeType);

        _result = $"({left} {varoperator} {right})";
        return node;
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        var methodName = node.Method.Name;

        // 集約関数の処理
        if (KsqlFunctionRegistry.IsAggregateFunction(methodName))
        {
            _result = ProcessAggregateFunction(node);
            return node;
        }

        // その他の関数
        _result = KsqlFunctionTranslator.TranslateMethodCall(node);
        return node;
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        // GROUP BYカラムの参照
        _result = node.Member.Name;
        return node;
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        _result = SafeToString(node.Value);
        return node;
    }

    protected override Expression VisitUnary(UnaryExpression node)
    {
        switch (node.NodeType)
        {
            case ExpressionType.Not:
                var operand = ProcessExpression(node.Operand);
                _result = $"NOT ({operand})";
                break;

            case ExpressionType.Convert:
            case ExpressionType.ConvertChecked:
                Visit(node.Operand);
                break;

            default:
                Visit(node.Operand);
                break;
        }

        return node;
    }

    /// <summary>
    /// 集約関数処理
    /// </summary>
    private string ProcessAggregateFunction(MethodCallExpression methodCall)
    {
        var methodName = methodCall.Method.Name;
        var ksqlFunction = TransformAggregateMethodName(methodName);

        // Count関数の特別処理
        if (methodName == "Count")
        {
            return ProcessCountFunction(methodCall, ksqlFunction);
        }

        // その他の集約関数
        return ProcessStandardAggregateFunction(methodCall, ksqlFunction);
    }

    /// <summary>
    /// Count関数処理
    /// </summary>
    private string ProcessCountFunction(MethodCallExpression methodCall, string ksqlFunction)
    {
        // Count() - 引数なし
        if (methodCall.Arguments.Count == 0)
        {
            return "COUNT(*)";
        }

        // Count(selector) - Lambda式の場合
        if (methodCall.Arguments.Count == 1 && methodCall.Arguments[0] is LambdaExpression)
        {
            return "COUNT(*)";
        }

        // Count(source) - ソース指定
        if (methodCall.Arguments.Count == 1)
        {
            return "COUNT(*)";
        }

        // Count(source, predicate) - 条件付きカウント（KSQL未対応）
        if (methodCall.Arguments.Count == 2)
        {
            throw new InvalidOperationException(
                "Conditional Count is not supported in KSQL HAVING clause. Use WHERE clause instead.");
        }

        return "COUNT(*)";
    }

    /// <summary>
    /// 標準集約関数処理
    /// </summary>
    private string ProcessStandardAggregateFunction(MethodCallExpression methodCall, string ksqlFunction)
    {
        // インスタンスメソッドの場合（g.Sum(x => x.Amount)）
        if (methodCall.Arguments.Count == 1 && methodCall.Arguments[0] is LambdaExpression lambda)
        {
            var columnName = ExtractColumnFromLambda(lambda);
            return $"{ksqlFunction}({columnName})";
        }

        // 静的メソッドの場合（extension method）
        if (methodCall.Method.IsStatic && methodCall.Arguments.Count >= 2)
        {
            var staticLambda = ExtractLambda(methodCall.Arguments[1]);
            if (staticLambda != null)
            {
                var columnName = ExtractColumnFromLambda(staticLambda);
                return $"{ksqlFunction}({columnName})";
            }
        }

        // オブジェクトメソッドの場合
        if (methodCall.Object is MemberExpression objMember)
        {
            return $"{ksqlFunction}({objMember.Member.Name})";
        }

        // フォールバック
        return $"{ksqlFunction}(*)";
    }

    /// <summary>
    /// Lambda式からカラム名抽出
    /// </summary>
    private string ExtractColumnFromLambda(LambdaExpression lambda)
    {
        return lambda.Body switch
        {
            MemberExpression member => member.Member.Name,
            UnaryExpression unary when unary.Operand is MemberExpression memberInner => memberInner.Member.Name,
            _ => throw new InvalidOperationException($"Cannot extract column name from lambda: {lambda}")
        };
    }

    /// <summary>
    /// Lambda式抽出
    /// </summary>
    private static LambdaExpression? ExtractLambda(Expression expr)
    {
        return expr switch
        {
            LambdaExpression lambda => lambda,
            UnaryExpression { Operand: LambdaExpression lambda } => lambda,
            _ => null
        };
    }

    /// <summary>
    /// 集約メソッド名変換
    /// </summary>
    private static string TransformAggregateMethodName(string methodName)
    {
        return methodName switch
        {
            "LatestByOffset" => "LATEST_BY_OFFSET",
            "EarliestByOffset" => "EARLIEST_BY_OFFSET",
            "CollectList" => "COLLECT_LIST",
            "CollectSet" => "COLLECT_SET",
            "Average" => "AVG",
            "CountDistinct" => "COUNT_DISTINCT",
            _ => methodName.ToUpper()
        };
    }

    /// <summary>
    /// 汎用式処理
    /// </summary>
    private string ProcessExpression(Expression expression)
    {
        return expression switch
        {
            MethodCallExpression methodCall when KsqlFunctionRegistry.IsAggregateFunction(methodCall.Method.Name)
                => ProcessAggregateFunction(methodCall),
            MethodCallExpression methodCall => KsqlFunctionTranslator.TranslateMethodCall(methodCall),
            MemberExpression member => member.Member.Name,
            ConstantExpression constant => SafeToString(constant.Value),
            BinaryExpression binary => ProcessBinaryExpression(binary),
            UnaryExpression unary => ProcessUnaryExpression(unary),
            _ => expression.ToString()
        };
    }

    /// <summary>
    /// 二項式処理
    /// </summary>
    private string ProcessBinaryExpression(BinaryExpression binary)
    {
        var left = ProcessExpression(binary.Left);
        var right = ProcessExpression(binary.Right);
        var varoperator = GetSqlOperator(binary.NodeType);
        return $"({left} {varoperator} {right})";
    }

    /// <summary>
    /// 単項式処理
    /// </summary>
    private string ProcessUnaryExpression(UnaryExpression unary)
    {
        return unary.NodeType switch
        {
            ExpressionType.Not => $"NOT ({ProcessExpression(unary.Operand)})",
            ExpressionType.Convert or ExpressionType.ConvertChecked => ProcessExpression(unary.Operand),
            _ => ProcessExpression(unary.Operand)
        };
    }

    /// <summary>
    /// SQL演算子変換
    /// </summary>
    private static string GetSqlOperator(ExpressionType nodeType)
    {
        return nodeType switch
        {
            ExpressionType.Equal => "=",
            ExpressionType.NotEqual => "<>",
            ExpressionType.GreaterThan => ">",
            ExpressionType.GreaterThanOrEqual => ">=",
            ExpressionType.LessThan => "<",
            ExpressionType.LessThanOrEqual => "<=",
            ExpressionType.AndAlso => "AND",
            ExpressionType.OrElse => "OR",
            ExpressionType.Add => "+",
            ExpressionType.Subtract => "-",
            ExpressionType.Multiply => "*",
            ExpressionType.Divide => "/",
            ExpressionType.Modulo => "%",
            _ => throw new NotSupportedException($"Operator {nodeType} is not supported in HAVING clause")
        };
    }

    /// <summary>
    /// NULL安全文字列変換
    /// </summary>
    private static string SafeToString(object? value)
    {
        return BuilderValidation.SafeToString(value);
    }
}
