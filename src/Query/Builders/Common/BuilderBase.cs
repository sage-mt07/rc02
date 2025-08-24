using Kafka.Ksql.Linq.Query.Abstractions;
using System;
using System.Linq.Expressions;

namespace Kafka.Ksql.Linq.Query.Builders.Common;

/// <summary>
/// Builder基底クラス
/// 設計理由：責務分離設計における共通制約・バリデーションの統一実装
/// 強制制約：readonly fields のみ、static メソッド推奨、Expression以外の外部参照禁止、副作用完全禁止
/// </summary>
internal abstract class BuilderBase : IKsqlBuilder
{
    /// <summary>
    /// ビルダー種別（派生クラスで実装必須）
    /// </summary>
    public abstract KsqlBuilderType BuilderType { get; }

    /// <summary>
    /// 式木からKSQL構文を構築（公開インターフェース）
    /// </summary>
    /// <param name="expression">対象式木</param>
    /// <returns>KSQL構文文字列</returns>
    public string Build(Expression expression)
    {
        // 共通バリデーション実行
        ValidateInput(expression);

        try
        {
            // 派生クラスの実装を呼び出し
            var result = BuildInternal(expression);

            // 結果バリデーション
            ValidateOutput(result);

            return result;
        }
        catch (Exception ex) when (!(ex is ArgumentException || ex is InvalidOperationException))
        {
            // 予期しないエラーをより具体的なエラーに変換
            throw new InvalidOperationException(
                $"Failed to build {BuilderType} clause from expression. " +
                $"Expression: {expression}. " +
                $"Error: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 派生クラス実装の本体（保護されたメソッド）
    /// </summary>
    /// <param name="expression">バリデーション済み式木</param>
    /// <returns>KSQL構文文字列</returns>
    protected abstract string BuildInternal(Expression expression);
    /// <summary>
    /// 必須Builderタイプ定義（派生クラスで実装）
    /// </summary>
    /// <returns>依存する他のBuilderタイプ配列</returns>
    protected virtual KsqlBuilderType[] GetRequiredBuilderTypes()
    {
        return Array.Empty<KsqlBuilderType>(); // デフォルトは依存なし
    }
    /// <summary>
    /// 入力バリデーション（共通処理）
    /// </summary>
    private void ValidateInput(Expression expression)
    {
        if (expression == null)
        {
            throw new ArgumentNullException(nameof(expression),
                $"{BuilderType} builder requires a non-null expression");
        }

        // 共通バリデーション実行
        BuilderValidation.ValidateExpression(expression);

        // ビルダー固有のバリデーション
        ValidateBuilderSpecific(expression);
    }

    /// <summary>
    /// 出力バリデーション（共通処理）
    /// </summary>
    private void ValidateOutput(string result)
    {
        if (string.IsNullOrWhiteSpace(result))
        {
            throw new InvalidOperationException(
                $"{BuilderType} builder produced empty result. " +
                "This indicates an issue with the expression processing logic.");
        }

        // SQLインジェクション基本チェック
        ValidateBasicSqlSafety(result);
    }

    /// <summary>
    /// ビルダー固有バリデーション（派生クラスでオーバーライド可能）
    /// </summary>
    /// <param name="expression">検証対象式木</param>
    protected virtual void ValidateBuilderSpecific(Expression expression)
    {
        // デフォルトは何もしない（派生クラスで必要に応じて実装）
    }

    /// <summary>
    /// SQL安全性基本チェック
    /// </summary>
    private static void ValidateBasicSqlSafety(string result)
    {
        // 基本的な危険パターンチェック
        var dangerousPatterns = new[]
        {
            "--", "/*", "*/", ";--", "';", "DROP", "DELETE", "INSERT", "UPDATE",
            "EXEC", "EXECUTE", "sp_", "xp_", "UNION", "SCRIPT"
        };

        var upperResult = result.ToUpper();
        foreach (var pattern in dangerousPatterns)
        {
            if (upperResult.Contains(pattern))
            {
                throw new InvalidOperationException(
                    $"Generated SQL contains potentially dangerous pattern: '{pattern}'. " +
                    $"Generated SQL: {result}");
            }
        }
    }

    /// <summary>
    /// 共通ヘルパー：MemberExpression安全抽出
    /// </summary>
    protected static MemberExpression? SafeExtractMember(Expression expression)
    {
        return BuilderValidation.ExtractMemberExpression(expression);
    }

    /// <summary>
    /// 共通ヘルパー：Lambda Body安全抽出
    /// </summary>
    protected static Expression? SafeExtractLambdaBody(Expression expression)
    {
        return BuilderValidation.ExtractLambdaBody(expression);
    }

    /// <summary>
    /// 共通ヘルパー：NULL安全文字列変換
    /// </summary>
    protected static string SafeToString(object? value)
    {
        return BuilderValidation.SafeToString(value);
    }

    /// <summary>
    /// 共通ヘルパー：式木型チェック
    /// </summary>
    protected static bool IsExpressionType<T>(Expression expression) where T : Expression
    {
        return expression is T;
    }

    /// <summary>
    /// 共通ヘルパー：メソッド名抽出
    /// </summary>
    protected static string? ExtractMethodName(Expression expression)
    {
        return expression is MethodCallExpression methodCall ? methodCall.Method.Name : null;
    }

    /// <summary>
    /// エラーメッセージ生成ヘルパー
    /// </summary>
    protected string CreateErrorMessage(string operation, Expression expression, Exception? innerException = null)
    {
        var message = $"{BuilderType} builder failed during {operation}. " +
                     $"Expression type: {expression.GetType().Name}. " +
                     $"Expression: {expression}";

        if (innerException != null)
        {
            message += $". Inner error: {innerException.Message}";
        }

        return message;
    }

    /// <summary>
    /// デバッグ情報生成（開発時使用）
    /// </summary>
    protected virtual string GetDebugInfo(Expression expression)
    {
        return $"Builder: {GetType().Name}, " +
               $"Type: {BuilderType}, " +
               $"Expression: {expression.GetType().Name}, " +
               $"NodeType: {expression.NodeType}";
    }

    /// <summary>
    /// ToString実装（デバッグ用）
    /// </summary>
    public override string ToString()
    {
        return $"{GetType().Name}[{BuilderType}]";
    }
}
