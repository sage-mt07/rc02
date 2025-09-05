using Kafka.Ksql.Linq.Query.Abstractions;
using Kafka.Ksql.Linq.Query.Builders.Common;
using System;
using System.Linq.Expressions;

namespace Kafka.Ksql.Linq.Query.Builders;

/// <summary>
/// Builder for SELECT clause content.
/// Rationale: separation-of-concerns; generate only clause content without keywords.
/// 出力例: "col1, col2 AS alias" (SELECT除外)
/// </summary>
internal class SelectClauseBuilder : BuilderBase
{
    public override KsqlBuilderType BuilderType => KsqlBuilderType.Select;
    private readonly System.Collections.Generic.IDictionary<string, string>? _paramToSource;
    private readonly System.Collections.Generic.ISet<string>? _excludeAliases;

    public SelectClauseBuilder() { }
    public SelectClauseBuilder(System.Collections.Generic.IDictionary<string, string> paramToSource)
    {
        _paramToSource = paramToSource;
    }
    public SelectClauseBuilder(System.Collections.Generic.IDictionary<string, string> paramToSource, System.Collections.Generic.ISet<string> excludeAliases)
    {
        _paramToSource = paramToSource;
        _excludeAliases = excludeAliases;
    }

    protected override KsqlBuilderType[] GetRequiredBuilderTypes()
    {
        return Array.Empty<KsqlBuilderType>(); // 他Builderに依存しない
    }

    protected override string BuildInternal(Expression expression)
    {
        var visitor = _paramToSource == null
            ? new SelectExpressionVisitor()
            : (_excludeAliases == null
                ? new SelectExpressionVisitor(_paramToSource)
                : new SelectExpressionVisitor(_paramToSource, _excludeAliases));
        visitor.Visit(expression);

        var result = visitor.GetResult();

        // 空の場合は * を返す
        return string.IsNullOrWhiteSpace(result) ? "*" : result;
    }

    public string BuildWithParamMap(System.Linq.Expressions.Expression expression, System.Collections.Generic.IDictionary<string, string> paramToSource)
    {
        var visitor = new SelectExpressionVisitor(paramToSource);
        visitor.Visit(expression);

        var result = visitor.GetResult();
        return string.IsNullOrWhiteSpace(result) ? "*" : result;
    }

    public string BuildWithParamMapAndExclude(System.Linq.Expressions.Expression expression,
        System.Collections.Generic.IDictionary<string, string> paramToSource,
        System.Collections.Generic.ISet<string> excludeAliases)
    {
        var visitor = new SelectExpressionVisitor(paramToSource, excludeAliases);
        visitor.Visit(expression);
        var result = visitor.GetResult();
        return string.IsNullOrWhiteSpace(result) ? "*" : result;
    }

    protected override void ValidateBuilderSpecific(Expression expression)
    {
        // SELECT-specific validation
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
