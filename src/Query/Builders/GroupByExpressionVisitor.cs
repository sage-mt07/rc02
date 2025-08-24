using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Kafka.Ksql.Linq.Query.Builders;
internal class GroupByExpressionVisitor : ExpressionVisitor
{
    private readonly List<string> _keys = new();

    public string GetResult()
    {
        return _keys.Count > 0 ? string.Join(", ", _keys) : string.Empty;
    }

    protected override Expression VisitNew(NewExpression node)
    {
        foreach (var arg in node.Arguments)
        {
            if (arg is NewExpression nested)
            {
                Visit(nested);
            }
            else
            {
                var key = ProcessKeyExpression(arg);
                _keys.Add(key);
            }
        }

        return node;
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        var key = ProcessKeyExpression(node);
        _keys.Add(key);
        return node;
    }

    protected override Expression VisitUnary(UnaryExpression node)
    {
        // 型変換の処理（Convert等）
        if (node.NodeType == ExpressionType.Convert || node.NodeType == ExpressionType.ConvertChecked)
        {
            return Visit(node.Operand);
        }

        return base.VisitUnary(node);
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        var methodName = node.Method.Name;

        if (IsAllowedGroupByFunction(methodName))
        {
            var functionCall = ProcessGroupByFunction(node);
            _keys.Add(functionCall);
            return node;
        }

        throw new InvalidOperationException(
            $"Function '{methodName}' is not allowed in GROUP BY clause");
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        var expression = ProcessBinaryExpression(node);
        _keys.Add(expression);
        return node;
    }

    private string ProcessKeyExpression(Expression expr)
    {
        if (expr is ConstantExpression)
        {
            throw new InvalidOperationException("Constant expression is not supported in GROUP BY");
        }

        return ProcessExpression(expr);
    }

    private string ProcessExpression(Expression expr)
    {
        return expr switch
        {
            MemberExpression member => GetMemberName(member),
            ConstantExpression constant => constant.Value?.ToString() ?? "NULL",
            UnaryExpression unary when unary.NodeType == ExpressionType.Convert || unary.NodeType == ExpressionType.ConvertChecked => ProcessExpression(unary.Operand),
            MethodCallExpression method when IsAllowedGroupByFunction(method.Method.Name) => ProcessGroupByFunction(method),
            BinaryExpression binary => ProcessBinaryExpression(binary),
            _ => throw new InvalidOperationException($"Expression type '{expr.GetType().Name}' is not supported in GROUP BY")
        };
    }

    private string ProcessBinaryExpression(BinaryExpression binary)
    {
        var left = ProcessExpression(binary.Left);
        var right = ProcessExpression(binary.Right);

        if (binary.NodeType == ExpressionType.Coalesce)
        {
            return $"COALESCE({left}, {right})";
        }

        var op = GetOperator(binary.NodeType);
        return $"{left} {op} {right}";
    }

    private static string GetOperator(ExpressionType nodeType)
    {
        return nodeType switch
        {
            ExpressionType.Add => "+",
            ExpressionType.Subtract => "-",
            ExpressionType.Multiply => "*",
            ExpressionType.Divide => "/",
            ExpressionType.Modulo => "%",
            ExpressionType.Equal => "=",
            ExpressionType.NotEqual => "<>",
            ExpressionType.GreaterThan => ">",
            ExpressionType.GreaterThanOrEqual => ">=",
            ExpressionType.LessThan => "<",
            ExpressionType.LessThanOrEqual => "<=",
            ExpressionType.AndAlso => "AND",
            ExpressionType.OrElse => "OR",
            _ => throw new NotSupportedException($"Operator {nodeType} is not supported in GROUP BY")
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
    /// GROUP BYで許可された関数判定
    /// </summary>
    private static bool IsAllowedGroupByFunction(string methodName)
    {
        var allowedFunctions = new[]
        {
            // 日付関数
            "Year", "Month", "Day", "Hour", "Minute", "Second",
            "DayOfWeek", "DayOfYear", "WeekOfYear",
            
            // 文字列関数（部分）
            "Substring", "Left", "Right", "ToUpper", "ToLower", "Upper", "Lower",
            
            // 数値関数（部分）
            "Floor", "Ceiling", "Round",
            
            // 型変換
            "ToString", "Cast"
        };

        return allowedFunctions.Contains(methodName);
    }

    /// <summary>
    /// GROUP BY関数処理
    /// </summary>
    private string ProcessGroupByFunction(MethodCallExpression methodCall)
    {
        var methodName = methodCall.Method.Name;

        return methodName switch
        {
            // 日付関数
            "Year" => ProcessDateFunction("YEAR", methodCall),
            "Month" => ProcessDateFunction("MONTH", methodCall),
            "Day" => ProcessDateFunction("DAY", methodCall),
            "Hour" => ProcessDateFunction("HOUR", methodCall),
            "Minute" => ProcessDateFunction("MINUTE", methodCall),
            "Second" => ProcessDateFunction("SECOND", methodCall),
            "DayOfWeek" => ProcessDateFunction("DAY_OF_WEEK", methodCall),
            "DayOfYear" => ProcessDateFunction("DAY_OF_YEAR", methodCall),
            "WeekOfYear" => ProcessDateFunction("WEEK_OF_YEAR", methodCall),

            // 文字列関数
            "Substring" => ProcessSubstringFunction(methodCall),
            "Left" => ProcessLeftFunction(methodCall),
            "Right" => ProcessRightFunction(methodCall),
            "ToUpper" => ProcessSimpleFunction("UPPER", methodCall),
            "ToLower" => ProcessSimpleFunction("LOWER", methodCall),
            "Upper" => ProcessSimpleFunction("UPPER", methodCall),
            "Lower" => ProcessSimpleFunction("LOWER", methodCall),

            // 数値関数
            "Floor" => ProcessSimpleFunction("FLOOR", methodCall),
            "Ceiling" => ProcessSimpleFunction("CEIL", methodCall),
            "Round" => ProcessRoundFunction(methodCall),

            // 型変換
            "ToString" => ProcessToStringFunction(methodCall),

            _ => throw new InvalidOperationException($"Unsupported GROUP BY function: {methodName}")
        };
    }

    /// <summary>
    /// 日付関数処理
    /// </summary>
    private string ProcessDateFunction(string ksqlFunction, MethodCallExpression methodCall)
    {
        var target = methodCall.Object ?? methodCall.Arguments[0];
        var columnName = ExtractColumnName(target);
        return $"{ksqlFunction}({columnName})";
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

    /// <summary>
    /// SUBSTRING関数処理
    /// </summary>
    private string ProcessSubstringFunction(MethodCallExpression methodCall)
    {
        var target = methodCall.Object ?? methodCall.Arguments[0];
        var columnName = ExtractColumnName(target);

        if (methodCall.Arguments.Count >= 1)
        {
            var startIndex = ExtractConstantValue(methodCall.Arguments[methodCall.Object != null ? 0 : 1]);

            var lengthIndex = methodCall.Object != null ? 1 : 2;
            if (methodCall.Arguments.Count > lengthIndex)
            {
                var length = ExtractConstantValue(methodCall.Arguments[lengthIndex]);
                return $"SUBSTRING({columnName}, {startIndex}, {length})";
            }

            return $"SUBSTRING({columnName}, {startIndex})";
        }

        throw new InvalidOperationException("SUBSTRING requires at least start index parameter");
    }

    /// <summary>
    /// LEFT関数処理
    /// </summary>
    private string ProcessLeftFunction(MethodCallExpression methodCall)
    {
        var target = methodCall.Object ?? methodCall.Arguments[0];
        var columnName = ExtractColumnName(target);
        var length = ExtractConstantValue(methodCall.Arguments[methodCall.Object != null ? 0 : 1]);
        return $"LEFT({columnName}, {length})";
    }

    /// <summary>
    /// RIGHT関数処理
    /// </summary>
    private string ProcessRightFunction(MethodCallExpression methodCall)
    {
        var target = methodCall.Object ?? methodCall.Arguments[0];
        var columnName = ExtractColumnName(target);
        var length = ExtractConstantValue(methodCall.Arguments[methodCall.Object != null ? 0 : 1]);
        return $"RIGHT({columnName}, {length})";
    }

    /// <summary>
    /// ROUND関数処理
    /// </summary>
    private string ProcessRoundFunction(MethodCallExpression methodCall)
    {
        var target = methodCall.Object ?? methodCall.Arguments[0];
        var columnName = ExtractColumnName(target);

        if (methodCall.Arguments.Count >= 2)
        {
            var precision = ExtractConstantValue(methodCall.Arguments[methodCall.Object != null ? 0 : 1]);
            return $"ROUND({columnName}, {precision})";
        }

        return $"ROUND({columnName})";
    }

    /// <summary>
    /// ToString関数処理
    /// </summary>
    private string ProcessToStringFunction(MethodCallExpression methodCall)
    {
        var target = methodCall.Object ?? methodCall.Arguments[0];
        var columnName = ExtractColumnName(target);
        return $"CAST({columnName} AS VARCHAR)";
    }

    /// <summary>
    /// カラム名抽出
    /// </summary>
    private string ExtractColumnName(Expression expression)
    {
        return expression switch
        {
            MemberExpression member => member.Member.Name,
            UnaryExpression unary => ExtractColumnName(unary.Operand),
            _ => throw new InvalidOperationException($"Cannot extract column name from {expression.GetType().Name}")
        };
    }

    /// <summary>
    /// 定数値抽出
    /// </summary>
    private string ExtractConstantValue(Expression expression)
    {
        return expression switch
        {
            ConstantExpression constant => constant.Value?.ToString() ?? "NULL",
            UnaryExpression unary => ExtractConstantValue(unary.Operand),
            _ => throw new InvalidOperationException($"Expected constant value but got {expression.GetType().Name}")
        };
    }
}
