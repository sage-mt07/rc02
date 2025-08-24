using Kafka.Ksql.Linq.Query.Abstractions;
using Kafka.Ksql.Linq.Query.Builders.Common;
using System;
using System.Linq.Expressions;
using System.Threading;

namespace Kafka.Ksql.Linq.Query.Builders;

/// <summary>
/// GROUP BY句内容構築ビルダー
/// 設計理由：責務分離設計に準拠、キーワード除外で純粋なグループ化キー内容のみ生成
/// 出力例: "col1, col2" (GROUP BY除外)
/// </summary>
internal class GroupByClauseBuilder : BuilderBase
{
    private static readonly AsyncLocal<Expression?> _lastGroupByExpression = new();

    internal static Expression? LastGroupByExpression
    {
        get => _lastGroupByExpression.Value;
        private set => _lastGroupByExpression.Value = value;
    }

    public override KsqlBuilderType BuilderType => KsqlBuilderType.GroupBy;

    protected override KsqlBuilderType[] GetRequiredBuilderTypes()
    {
        return Array.Empty<KsqlBuilderType>(); // 他Builderに依存しない
    }

    protected override string BuildInternal(Expression expression)
    {
        LastGroupByExpression = expression;
        var visitor = new GroupByExpressionVisitor();
        visitor.Visit(expression);

        var result = visitor.GetResult();

        if (string.IsNullOrWhiteSpace(result))
        {
            throw new InvalidOperationException("Unable to extract GROUP BY keys from expression");
        }

        return result;
    }

    protected override void ValidateBuilderSpecific(Expression expression)
    {
        // GROUP BY句特有のバリデーション
        ValidateNoAggregateInGroupBy(expression);
        ValidateGroupByKeyCount(expression);
    }

    /// <summary>
    /// GROUP BY句での集約関数使用禁止チェック
    /// </summary>
    private static void ValidateNoAggregateInGroupBy(Expression expression)
    {
        var visitor = new AggregateDetectionVisitor();
        visitor.Visit(expression);

        if (visitor.HasAggregates)
        {
            throw new InvalidOperationException(
                "Aggregate functions are not allowed in GROUP BY clause");
        }
    }

    /// <summary>
    /// GROUP BYキー数制限チェック
    /// </summary>
    private static void ValidateGroupByKeyCount(Expression expression)
    {
        var visitor = new GroupByKeyCountVisitor();
        visitor.Visit(expression);

        const int maxKeys = 10; // KSQL推奨制限
        if (visitor.KeyCount > maxKeys)
        {
            throw new InvalidOperationException(
                $"GROUP BY supports maximum {maxKeys} keys for optimal performance. " +
                $"Found {visitor.KeyCount} keys. Consider using composite keys or data denormalization.");
        }
    }
}
