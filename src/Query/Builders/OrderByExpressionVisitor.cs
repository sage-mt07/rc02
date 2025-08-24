using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Kafka.Ksql.Linq.Query.Builders;
internal class OrderByExpressionVisitor : ExpressionVisitor
{
    private readonly List<string> _orderClauses = new();

    public string GetResult()
    {
        return _orderClauses.Count > 0 ? string.Join(", ", _orderClauses) : string.Empty;
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        var methodName = node.Method.Name;

        switch (methodName)
        {
            case "OrderBy":
                ProcessOrderByCall(node, "ASC");
                break;

            case "OrderByDescending":
                ProcessOrderByCall(node, "DESC");
                break;

            case "ThenBy":
                ProcessThenByCall(node, "ASC");
                break;

            case "ThenByDescending":
                ProcessThenByCall(node, "DESC");
                break;

            default:
                // 他のメソッド呼び出しは再帰的に処理
                base.VisitMethodCall(node);
                break;
        }

        return node;
    }

    /// <summary>
    /// OrderBy/OrderByDescending 処理
    /// </summary>
    private void ProcessOrderByCall(MethodCallExpression node, string direction)
    {
        // 前のメソッドチェーンを先に処理
        if (node.Object != null)
        {
            Visit(node.Object);
        }

        // 現在のOrderBy処理
        if (node.Arguments.Count >= 2)
        {
            var keySelector = ExtractLambdaExpression(node.Arguments[1]);
            if (keySelector != null)
            {
                var columnName = ExtractColumnName(keySelector.Body);
                _orderClauses.Add($"{columnName} {direction}");
            }
        }
    }

    /// <summary>
    /// ThenBy/ThenByDescending 処理
    /// </summary>
    private void ProcessThenByCall(MethodCallExpression node, string direction)
    {
        // 前のメソッドチェーンを先に処理
        if (node.Object != null)
        {
            Visit(node.Object);
        }

        // 現在のThenBy処理
        if (node.Arguments.Count >= 2)
        {
            var keySelector = ExtractLambdaExpression(node.Arguments[1]);
            if (keySelector != null)
            {
                var columnName = ExtractColumnName(keySelector.Body);
                _orderClauses.Add($"{columnName} {direction}");
            }
        }
    }

    /// <summary>
    /// Lambda式抽出
    /// </summary>
    private static LambdaExpression? ExtractLambdaExpression(Expression expr)
    {
        return expr switch
        {
            UnaryExpression unary when unary.Operand is LambdaExpression lambda => lambda,
            LambdaExpression lambda => lambda,
            _ => null
        };
    }

    /// <summary>
    /// カラム名抽出
    /// </summary>
    private string ExtractColumnName(Expression expr)
    {
        return expr switch
        {
            MemberExpression member => GetMemberName(member),
            UnaryExpression unary when unary.NodeType == ExpressionType.Convert => ExtractColumnName(unary.Operand),
            MethodCallExpression method => ProcessOrderByFunction(method),
            _ => throw new InvalidOperationException($"Unsupported ORDER BY expression: {expr.GetType().Name}")
        };
    }

    /// <summary>
    /// メンバー名取得
    /// </summary>
    private static string GetMemberName(MemberExpression member)
    {
        // ネストしたプロパティの場合は最下位のプロパティ名を使用
        return member.Member.Name;
    }

    /// <summary>
    /// ORDER BY関数処理
    /// </summary>
    private string ProcessOrderByFunction(MethodCallExpression methodCall)
    {
        var methodName = methodCall.Method.Name;

        // ORDER BYで許可される関数は限定的
        return methodName switch
        {
            // ウィンドウ関数
            "RowTime" => "ROWTIME",

            // 文字列関数（部分的）
            "ToUpper" => ProcessSimpleFunction("UPPER", methodCall),
            "ToLower" => ProcessSimpleFunction("LOWER", methodCall),

            // 数値関数（部分的）
            "Abs" => ProcessSimpleFunction("ABS", methodCall),

            // 日付関数（部分的）
            "Year" => ProcessSimpleFunction("YEAR", methodCall),
            "Month" => ProcessSimpleFunction("MONTH", methodCall),
            "Day" => ProcessSimpleFunction("DAY", methodCall),

            _ => throw new InvalidOperationException($"Function '{methodName}' is not supported in ORDER BY clause")
        };
    }

    /// <summary>
    /// 単純関数処理
    /// </summary>
    private string ProcessSimpleFunction(string ksqlFunction, MethodCallExpression methodCall)
    {
        var target = methodCall.Object ?? methodCall.Arguments[0];
        var columnName = ExtractColumnName(target);
        return $"{ksqlFunction}({columnName})";
    }
}

/// <summary>
