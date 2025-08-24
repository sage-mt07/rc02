using Kafka.Ksql.Linq.Query.Abstractions;
using Kafka.Ksql.Linq.Query.Builders.Common;
using System;
using System.Linq.Expressions;

namespace Kafka.Ksql.Linq.Query.Builders;

/// <summary>
/// SELECT句内容構築ビルダー
/// 設計理由：責務分離設計に準拠、キーワード除外で純粋な句内容のみ生成
/// 出力例: "col1, col2 AS alias" (SELECT除外)
/// </summary>
internal class SelectClauseBuilder : BuilderBase
{
    public override KsqlBuilderType BuilderType => KsqlBuilderType.Select;

    protected override KsqlBuilderType[] GetRequiredBuilderTypes()
    {
        return Array.Empty<KsqlBuilderType>(); // 他Builderに依存しない
    }

    protected override string BuildInternal(Expression expression)
    {
        var visitor = new SelectExpressionVisitor();
        visitor.Visit(expression);

        var result = visitor.GetResult();

        // 空の場合は * を返す
        return string.IsNullOrWhiteSpace(result) ? "*" : result;
    }

    protected override void ValidateBuilderSpecific(Expression expression)
    {
        // SELECT句特有のバリデーション
        BuilderValidation.ValidateNoNestedAggregates(expression);

        if (expression is MethodCallExpression)
        {
            // 集約関数の混在チェック
            if (ContainsAggregateFunction(expression) && ContainsNonAggregateColumns(expression))
            {
                throw new InvalidOperationException(
                    "SELECT clause cannot mix aggregate functions with non-aggregate columns without GROUP BY");
            }
        }
    }

    /// <summary>
    /// 集約関数含有チェック
    /// </summary>
    private static bool ContainsAggregateFunction(Expression expression)
    {
        var visitor = new AggregateDetectionVisitor();
        visitor.Visit(expression);
        return visitor.HasAggregates;
    }

    /// <summary>
    /// 非集約カラム含有チェック
    /// </summary>
    private static bool ContainsNonAggregateColumns(Expression expression)
    {
        var visitor = new NonAggregateColumnVisitor();
        visitor.Visit(expression);
        return visitor.HasNonAggregateColumns;
    }
}
