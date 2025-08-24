using Kafka.Ksql.Linq.Query.Abstractions;
using Kafka.Ksql.Linq.Query.Builders.Common;
using System;
using System.Linq.Expressions;

namespace Kafka.Ksql.Linq.Query.Builders;

/// <summary>
/// HAVING句内容構築ビルダー
/// 設計理由：責務分離設計に準拠、キーワード除外で純粋な集約条件内容のみ生成
/// 出力例: "SUM(amount) > 100 AND COUNT(*) > 5" (HAVING除外)
/// </summary>
internal class HavingClauseBuilder : BuilderBase
{
    public override KsqlBuilderType BuilderType => KsqlBuilderType.Having;

    protected override KsqlBuilderType[] GetRequiredBuilderTypes()
    {
        return Array.Empty<KsqlBuilderType>(); // 他Builderに依存しない
    }

    protected override string BuildInternal(Expression expression)
    {
        var visitor = new HavingExpressionVisitor();
        visitor.Visit(expression);
        return visitor.GetResult();
    }

    protected override void ValidateBuilderSpecific(Expression expression)
    {
        // HAVING句特有のバリデーション
        BuilderValidation.ValidateNoNestedAggregates(expression);
        ValidateRequiresAggregateOrGroupByColumn(expression);
    }

    /// <summary>
    /// HAVING句では集約関数またはGROUP BYカラムのみ許可
    /// </summary>
    private static void ValidateRequiresAggregateOrGroupByColumn(Expression expression)
    {
        var visitor = new HavingValidationVisitor();
        visitor.Visit(expression);

        if (visitor.HasInvalidReferences)
        {
            throw new InvalidOperationException(
                "HAVING clause can only reference aggregate functions or columns in GROUP BY clause");
        }
    }
}
