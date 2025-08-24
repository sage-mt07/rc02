using Kafka.Ksql.Linq.Query.Abstractions;
using Kafka.Ksql.Linq.Query.Builders.Common;
using System;
using System.Linq.Expressions;

namespace Kafka.Ksql.Linq.Query.Builders;

/// <summary>
/// WHERE句内容構築ビルダー
/// 設計理由：責務分離設計に準拠、キーワード除外で純粋な条件内容のみ生成
/// 出力例: "condition1 AND condition2" (WHERE除外)
/// </summary>
internal class WhereClauseBuilder : BuilderBase
{
    public override KsqlBuilderType BuilderType => KsqlBuilderType.Where;

    protected override KsqlBuilderType[] GetRequiredBuilderTypes()
    {
        return Array.Empty<KsqlBuilderType>(); // 他Builderに依存しない
    }

    protected override string BuildInternal(Expression expression)
    {
        var visitor = new WhereExpressionVisitor();
        visitor.Visit(expression);
        return visitor.GetResult();
    }

    protected override void ValidateBuilderSpecific(Expression expression)
    {
        // WHERE句特有のバリデーション
        ValidateNoAggregateInWhere(expression);
        ValidateNoSelectStatements(expression);
    }

    /// <summary>
    /// WHERE句での集約関数使用禁止チェック
    /// </summary>
    private static void ValidateNoAggregateInWhere(Expression expression)
    {
        var visitor = new AggregateDetectionVisitor();
        visitor.Visit(expression);

        if (visitor.HasAggregates)
        {
            throw new InvalidOperationException(
                "Aggregate functions are not allowed in WHERE clause. Use HAVING clause instead.");
        }
    }

    /// <summary>
    /// WHERE句でのSELECT文混入禁止チェック
    /// </summary>
    private static void ValidateNoSelectStatements(Expression expression)
    {
        // 基本的なサブクエリパターンの検出
        var expressionString = expression.ToString().ToUpper();
        if (expressionString.Contains("SELECT"))
        {
            throw new InvalidOperationException(
                "Subqueries are not supported in WHERE clause in KSQL");
        }
    }

    /// <summary>
    /// 条件のみ構築（WHERE プレフィックスなし）
    /// </summary>
    public string BuildCondition(Expression expression)
    {
        return BuildInternal(expression);
    }
}
