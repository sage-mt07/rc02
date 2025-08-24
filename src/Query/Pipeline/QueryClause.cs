using System.Linq.Expressions;

namespace Kafka.Ksql.Linq.Query.Pipeline;

/// <summary>
/// クエリ句定義
/// </summary>
internal record QueryClause(
    QueryClauseType Type,
    string Content,
    Expression? SourceExpression = null,
    int Priority = 0)
{
    /// <summary>
    /// 必須句作成
    /// </summary>
    public static QueryClause Required(QueryClauseType type, string content, Expression? sourceExpression = null)
    {
        return new QueryClause(type, content, sourceExpression, 100);
    }

    /// <summary>
    /// 任意句作成
    /// </summary>
    public static QueryClause Optional(QueryClauseType type, string content, Expression? sourceExpression = null)
    {
        return new QueryClause(type, content, sourceExpression, 50);
    }

    /// <summary>
    /// 空句判定
    /// </summary>
    public bool IsEmpty => string.IsNullOrWhiteSpace(Content);

    /// <summary>
    /// 有効句判定
    /// </summary>
    public bool IsValid => !IsEmpty;
}

/// <summary>
