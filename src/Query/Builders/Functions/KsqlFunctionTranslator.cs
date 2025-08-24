using Kafka.Ksql.Linq.Query.Builders.Common;
using Kafka.Ksql.Linq.Configuration;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Kafka.Ksql.Linq.Query.Builders.Functions;

/// <summary>
/// KSQL関数変換エンジン
/// 設計理由：C#メソッド呼び出しをKSQL関数呼び出しに変換する中核エンジン
/// </summary>
internal static class KsqlFunctionTranslator
{
    private static readonly Dictionary<string, HashSet<string>> _functionTypeMatrix = new()
    {
        ["SUM"] = new(["INT", "BIGINT", "DOUBLE"]),
        ["AVG"] = new(["INT", "BIGINT", "DOUBLE"]),
        ["MIN"] = new(["INT", "BIGINT", "DOUBLE", "DECIMAL", "STRING", "BOOLEAN", "DATETIME"]),
        ["MAX"] = new(["INT", "BIGINT", "DOUBLE", "DECIMAL", "STRING", "BOOLEAN", "DATETIME"]),
        ["COUNT"] = new(["INT", "BIGINT", "DOUBLE", "DECIMAL", "STRING", "BOOLEAN", "DATETIME", "STRUCT"]),
        ["TOPK"] = new(["INT", "BIGINT", "DOUBLE", "DECIMAL", "STRING", "BOOLEAN", "DATETIME"]),
        ["COLLECT_LIST"] = new(["INT", "BIGINT", "DOUBLE", "DECIMAL", "STRING", "BOOLEAN", "DATETIME", "STRUCT"]),
        ["LOWER"] = new(["STRING"]),
        ["UPPER"] = new(["STRING"]),
        ["LEN"] = new(["STRING"])
    };

    /// <summary>
    /// メソッド呼び出しをKSQL関数に変換
    /// </summary>
    public static string TranslateMethodCall(MethodCallExpression methodCall)
    {
        BuilderValidation.ValidateExpression(methodCall);

        var methodName = methodCall.Method.Name;
        var mapping = KsqlFunctionRegistry.GetMapping(methodName);

        if (mapping == null)
        {
            return HandleUnknownMethod(methodCall);
        }

        // 引数数検証
        var argCount = GetEffectiveArgumentCount(methodCall);
        if (!mapping.IsValidArgCount(argCount))
        {
            throw new ArgumentException(
                $"Method '{methodName}' expects {mapping.MinArgs}-{mapping.MaxArgs} arguments, but got {argCount}");
        }

        ValidateTypeCompatibility(mapping.KsqlFunction, ExtractArgumentTypes(methodCall));

        // 特殊処理が必要な場合
        if (mapping.RequiresSpecialHandling)
        {
            return HandleSpecialFunction(methodCall, mapping);
        }

        // 通常の関数変換
        return TranslateStandardFunction(methodCall, mapping);
    }

    /// <summary>
    /// 標準関数変換
    /// </summary>
    private static string TranslateStandardFunction(MethodCallExpression methodCall, KsqlFunctionMapping mapping)
    {
        var args = ExtractArguments(methodCall);
        return mapping.GenerateStandardCall(args.ToArray());
    }

    /// <summary>
    /// 特殊関数処理
    /// </summary>
    private static string HandleSpecialFunction(MethodCallExpression methodCall, KsqlFunctionMapping mapping)
    {
        var methodName = methodCall.Method.Name;

        return methodName switch
        {
            "ToString" => HandleToStringConversion(methodCall),
            "Parse" => HandleParseConversion(methodCall),
            "Convert" => HandleConvertConversion(methodCall),
            "Case" => HandleCaseExpression(methodCall),
            "Count" => HandleCountFunction(methodCall),
            _ => TranslateStandardFunction(methodCall, mapping)
        };
    }

    /// <summary>
    /// ToString変換処理
    /// </summary>
    private static string HandleToStringConversion(MethodCallExpression methodCall)
    {
        var args = ExtractArguments(methodCall);
        if (args.Count == 0 && methodCall.Object != null)
        {
            var objectArg = TranslateExpression(methodCall.Object);
            return $"CAST({objectArg} AS VARCHAR)";
        }

        return $"CAST({args[0]} AS VARCHAR)";
    }

    /// <summary>
    /// Parse変換処理
    /// </summary>
    private static string HandleParseConversion(MethodCallExpression methodCall)
    {
        var targetType = methodCall.Method.ReturnType;
        var ksqlType = MapToKsqlType(targetType);
        var args = ExtractArguments(methodCall);

        return $"CAST({args[0]} AS {ksqlType})";
    }

    /// <summary>
    /// Convert変換処理
    /// </summary>
    private static string HandleConvertConversion(MethodCallExpression methodCall)
    {
        // Convert.ToXxx(value) パターン
        if (methodCall.Method.DeclaringType == typeof(Convert))
        {
            var methodName = methodCall.Method.Name;
            var ksqlType = methodName switch
            {
                "ToInt32" => "INTEGER",
                "ToInt64" => "BIGINT",
                "ToDouble" => "DOUBLE",
                "ToDecimal" => "DECIMAL",
                "ToString" => "VARCHAR",
                "ToBoolean" => "BOOLEAN",
                _ => "VARCHAR"
            };

            var args = ExtractArguments(methodCall);
            return $"CAST({args[0]} AS {ksqlType})";
        }

        // 通常のConvert処理
        var arguments = ExtractArguments(methodCall);
        return $"CAST({arguments[0]} AS {arguments[1]})";
    }

    /// <summary>
    /// Case式処理
    /// </summary>
    private static string HandleCaseExpression(MethodCallExpression methodCall)
    {
        var args = ExtractArguments(methodCall);
        var result = new StringBuilder("CASE");

        for (int i = 0; i < args.Count - 1; i += 2)
        {
            result.Append($" WHEN {args[i]} THEN {args[i + 1]}");
        }

        // ELSE節（奇数個の引数の場合）
        if (args.Count % 2 == 1)
        {
            result.Append($" ELSE {args[args.Count - 1]}");
        }

        result.Append(" END");
        return result.ToString();
    }

    /// <summary>
    /// Count関数特殊処理
    /// </summary>
    private static string HandleCountFunction(MethodCallExpression methodCall)
    {
        var args = ExtractArguments(methodCall);

        // Count() - 引数なし
        if (args.Count == 0)
        {
            return "COUNT(*)";
        }

        // Count(selector) - Lambda式
        if (args.Count == 1)
        {
            // Lambda式の場合は引数を無視してCOUNT(*)
            if (methodCall.Arguments[0] is LambdaExpression)
            {
                return "COUNT(*)";
            }

            return $"COUNT({args[0]})";
        }

        // Count(source, predicate) - 条件付きカウント
        return $"COUNT({args[0]})";
    }

    /// <summary>
    /// 不明メソッド処理
    /// </summary>
    private static string HandleUnknownMethod(MethodCallExpression methodCall)
    {
        var methodName = methodCall.Method.Name;
        var args = ExtractArguments(methodCall);

        // 共通パターン推測
        if (methodName.StartsWith("To") && args.Count <= 1)
        {
            // ToXxx系メソッドをCAST変換として処理
            var targetType = InferTypeFromMethodName(methodName);
            var sourceArg = args.Count > 0 ? args[0] :
                           methodCall.Object != null ? TranslateExpression(methodCall.Object) : "NULL";
            return $"CAST({sourceArg} AS {targetType})";
        }

        throw new NotSupportedException($"Function '{methodName}' is not supported.");
    }

    /// <summary>
    /// 引数抽出
    /// </summary>
    private static List<string> ExtractArguments(MethodCallExpression methodCall)
    {
        var args = new List<string>();

        // インスタンスメソッドの場合、Objectも引数として扱う
        if (methodCall.Object != null && !methodCall.Method.IsStatic)
        {
            args.Add(TranslateExpression(methodCall.Object));
        }

        // 通常の引数
        foreach (var arg in methodCall.Arguments)
        {
            args.Add(TranslateExpression(arg));
        }

        // 拡張メソッドは最初の引数がレシーバなので除外
        if (methodCall.Method.IsStatic &&
            methodCall.Method.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false) &&
            args.Count > 0)
        {
            args.RemoveAt(0);
        }

        return args;
    }

    /// <summary>
    /// 実効引数数取得
    /// </summary>
    private static int GetEffectiveArgumentCount(MethodCallExpression methodCall)
    {
        var count = methodCall.Arguments.Count;

        // インスタンスメソッドの場合、Objectも1つの引数としてカウント
        if (methodCall.Object != null && !methodCall.Method.IsStatic)
        {
            count++;
        }

        // 拡張メソッドは第1引数がレシーバに相当するため引数から除外する
        if (methodCall.Method.IsStatic && methodCall.Method.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false))
        {
            count--;
        }

        return count;
    }

    /// <summary>
    /// 式変換（再帰処理）
    /// </summary>
    private static string TranslateExpression(Expression expression)
    {
        return expression switch
        {
            MethodCallExpression methodCall => TranslateMethodCall(methodCall),
            MemberExpression member => member.Member.Name,
            ConstantExpression constant => BuilderValidation.SafeToString(constant.Value),
            ParameterExpression parameter => parameter.Name ?? "param",
            LambdaExpression lambda => TranslateExpression(lambda.Body),
            UnaryExpression unary => TranslateExpression(unary.Operand),
            BinaryExpression binary => $"({TranslateExpression(binary.Left)} {GetOperator(binary.NodeType)} {TranslateExpression(binary.Right)})",
            ConditionalExpression conditional => TranslateConditionalExpression(conditional),
            _ => expression.ToString()
        };
    }

    /// <summary>
    /// 条件式変換
    /// </summary>
    private static string TranslateConditionalExpression(ConditionalExpression conditional)
    {
        BuilderValidation.ValidateConditionalTypes(conditional.IfTrue, conditional.IfFalse);

        var test = TranslateExpression(conditional.Test);
        var ifTrue = TranslateExpression(conditional.IfTrue);
        var ifFalse = TranslateExpression(conditional.IfFalse);
        return $"CASE WHEN {test} THEN {ifTrue} ELSE {ifFalse} END";
    }

    /// <summary>
    /// 二項演算子変換
    /// </summary>
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
            ExpressionType.NotEqual => "!=",
            ExpressionType.GreaterThan => ">",
            ExpressionType.GreaterThanOrEqual => ">=",
            ExpressionType.LessThan => "<",
            ExpressionType.LessThanOrEqual => "<=",
            ExpressionType.AndAlso => "AND",
            ExpressionType.OrElse => "OR",
            _ => throw new NotSupportedException($"ExpressionType '{nodeType}' is not supported.")
        };
    }

    /// <summary>
    /// C#型からKSQL型へのマッピング
    /// </summary>
    private static string MapToKsqlType(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        return underlyingType switch
        {
            Type t when t == typeof(int) => "INTEGER",
            Type t when t == typeof(short) => "INTEGER",
            Type t when t == typeof(long) => "BIGINT",
            Type t when t == typeof(double) => "DOUBLE",
            Type t when t == typeof(float) => "DOUBLE",
            Type t when t == typeof(decimal) => $"DECIMAL({DecimalPrecisionConfig.DecimalPrecision}, {DecimalPrecisionConfig.DecimalScale})",
            Type t when t == typeof(string) => "VARCHAR",
            Type t when t == typeof(bool) => "BOOLEAN",
            Type t when t == typeof(DateTime) => "TIMESTAMP",
            Type t when t == typeof(DateTimeOffset) => "TIMESTAMP",
            Type t when t == typeof(Guid) => "VARCHAR",
            Type t when t == typeof(byte[]) => "BYTES",
            _ when underlyingType.IsEnum => throw new NotSupportedException($"Type '{underlyingType.Name}' is not supported."),
            _ when !underlyingType.IsPrimitive && underlyingType != typeof(string) && underlyingType != typeof(Guid) && underlyingType != typeof(byte[]) => throw new NotSupportedException($"Type '{underlyingType.Name}' is not supported."),
            _ => throw new NotSupportedException($"Type '{underlyingType.Name}' is not supported.")
        };
    }

    private static string GetTypeCategory(Type type)
    {
        var t = Nullable.GetUnderlyingType(type) ?? type;
        if (t == typeof(int) || t == typeof(short)) return "INT";
        if (t == typeof(long)) return "BIGINT";
        if (t == typeof(double) || t == typeof(float)) return "DOUBLE";
        if (t == typeof(decimal)) return "DECIMAL";
        if (t == typeof(string) || t == typeof(char)) return "STRING";
        if (t == typeof(bool)) return "BOOLEAN";
        if (t == typeof(DateTime) || t == typeof(DateTimeOffset)) return "DATETIME";
        if (!t.IsPrimitive && t != typeof(string)) return "STRUCT";
        return "UNKNOWN";
    }

    private static IEnumerable<Type> ExtractArgumentTypes(MethodCallExpression methodCall)
    {
        var types = new List<Type>();
        if (methodCall.Object != null && !methodCall.Method.IsStatic)
        {
            types.Add(methodCall.Object.Type);
        }

        foreach (var arg in methodCall.Arguments)
        {
            if (arg is LambdaExpression lambda)
            {
                var body = BuilderValidation.ExtractLambdaBody(lambda);
                if (body != null)
                    types.Add(body.Type);
                else
                    types.Add(lambda.Type);
            }
            else
            {
                types.Add(arg.Type);
            }
        }

        if (methodCall.Method.IsStatic && methodCall.Method.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false) && types.Count > 0)
        {
            types.RemoveAt(0);
        }

        return types;
    }

    private static void ValidateTypeCompatibility(string functionName, IEnumerable<Type> argTypes)
    {
        if (!_functionTypeMatrix.TryGetValue(functionName.ToUpperInvariant(), out var allowed))
            return;

        foreach (var t in argTypes)
        {
            var category = GetTypeCategory(t);
            if (!allowed.Contains(category))
            {
                throw new NotSupportedException($"Function '{functionName}' does not support argument type {t.Name}");
            }
        }
    }

    /// <summary>
    /// メソッド名から型推測
    /// </summary>
    private static string InferTypeFromMethodName(string methodName)
    {
        var name = methodName.ToUpperInvariant();

        return name switch
        {
            "SUM" => "DOUBLE",
            "AVG" => "DOUBLE",
            "COUNT" => "BIGINT",
            "MAX" => "ANY",
            "MIN" => "ANY",
            "TOPK" => "ARRAY",
            "HISTOGRAM" => "MAP",
            "TOINT" or "TOINT32" => "INTEGER",
            "TOLONG" or "TOINT64" => "BIGINT",
            "TODOUBLE" => "DOUBLE",
            "TODECIMAL" => $"DECIMAL({DecimalPrecisionConfig.DecimalPrecision}, {DecimalPrecisionConfig.DecimalScale})",
            "TOSTRING" => "VARCHAR",
            "TOBOOL" or "TOBOOLEAN" => "BOOLEAN",
            _ => "UNKNOWN"
        };
    }

    /// <summary>
    /// デバッグ用：変換過程の情報出力
    /// </summary>
    public static string GetTranslationDebugInfo(MethodCallExpression methodCall)
    {
        var result = new StringBuilder();
        result.AppendLine($"Method: {methodCall.Method.Name}");
        result.AppendLine($"Declaring Type: {methodCall.Method.DeclaringType?.Name}");
        result.AppendLine($"Return Type: {methodCall.Method.ReturnType.Name}");
        result.AppendLine($"Is Static: {methodCall.Method.IsStatic}");
        result.AppendLine($"Object: {methodCall.Object?.Type.Name ?? "null"}");
        result.AppendLine($"Arguments: {methodCall.Arguments.Count}");

        for (int i = 0; i < methodCall.Arguments.Count; i++)
        {
            result.AppendLine($"  Arg[{i}]: {methodCall.Arguments[i].Type.Name} - {methodCall.Arguments[i]}");
        }

        var mapping = KsqlFunctionRegistry.GetMapping(methodCall.Method.Name);
        if (mapping != null)
        {
            result.AppendLine($"KSQL Mapping: {mapping.KsqlFunction}");
            result.AppendLine($"Args Range: {mapping.MinArgs}-{mapping.MaxArgs}");
            result.AppendLine($"Special Handling: {mapping.RequiresSpecialHandling}");
        }
        else
        {
            result.AppendLine("KSQL Mapping: Not found");
        }

        return result.ToString();
    }
}
