using Kafka.Ksql.Linq.Query.Abstractions;
using Kafka.Ksql.Linq.Query.Builders.Common;
using System;
using System.Linq.Expressions;

namespace Kafka.Ksql.Linq.Query.Builders;

/// <summary>
/// JOIN句構築ビルダー（3テーブル制限版）
/// 設計理由：責務分離設計に準拠、完全なJOIN文出力（キーワード含む）
/// 出力例: "JOIN table2 t2 ON t1.key = t2.key"
/// </summary>
internal class JoinClauseBuilder : BuilderBase
{
    public override KsqlBuilderType BuilderType => KsqlBuilderType.Join;

    protected override KsqlBuilderType[] GetRequiredBuilderTypes()
    {
        return Array.Empty<KsqlBuilderType>(); // 他Builderに依存しない
    }

    protected override string BuildInternal(Expression expression)
    {
        // JOIN制限の事前チェック
        JoinLimitationEnforcer.ValidateJoinExpression(expression);

        var visitor = new JoinExpressionVisitor();
        visitor.Visit(expression);

        var result = visitor.GetResult();

        if (string.IsNullOrWhiteSpace(result))
        {
            return "/* UNSUPPORTED JOIN PATTERN */";
        }

        return result;
    }

    protected override void ValidateBuilderSpecific(Expression expression)
    {
        // JOIN句特有のバリデーション
        ValidateJoinStructure(expression);
        ValidateJoinTypes(expression);
    }

    /// <summary>
    /// JOIN構造バリデーション
    /// </summary>
    private static void ValidateJoinStructure(Expression expression)
    {
        var joinCall = FindJoinCall(expression);
        if (joinCall == null)
        {
            throw new InvalidOperationException("Expression does not contain a valid JOIN operation");
        }

        // JOIN引数数チェック（outer, inner, outerKeySelector, innerKeySelector, resultSelector）
        if (joinCall.Arguments.Count < 4)
        {
            throw new InvalidOperationException(
                $"JOIN operation requires at least 4 arguments, but got {joinCall.Arguments.Count}");
        }
    }

    /// <summary>
    /// JOIN型バリデーション
    /// </summary>
    private static void ValidateJoinTypes(Expression expression)
    {
        // JoinLimitationEnforcer で既にチェック済みだが、追加チェック
        var joinCall = FindJoinCall(expression);
        if (joinCall?.Method.Name != "Join")
        {
            throw new InvalidOperationException(
                "Only INNER JOIN is supported. Use Join() method for INNER JOIN operations.");
        }
    }

    /// <summary>
    /// JOIN呼び出し検索
    /// </summary>
    private static MethodCallExpression? FindJoinCall(Expression expr)
    {
        return expr switch
        {
            MethodCallExpression mce when mce.Method.Name == "Join" => mce,
            LambdaExpression le => FindJoinCall(le.Body),
            UnaryExpression ue => FindJoinCall(ue.Operand),
            InvocationExpression ie => FindJoinCall(ie.Expression),
            _ => null
        };
    }
}
