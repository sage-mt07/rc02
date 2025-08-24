namespace Kafka.Ksql.Linq.Query.Pipeline;
/// <summary>
/// クエリパーツ情報
/// 設計理由：Generator層でのKSQL文構成部品管理
/// </summary>
internal record QueryPart(
    string Content,
    bool IsRequired,
    int Order = 0)
{
    /// <summary>
    /// 空のクエリパーツ
    /// </summary>
    public static QueryPart Empty => new(string.Empty, false);

    /// <summary>
    /// 必須クエリパーツ作成
    /// </summary>
    public static QueryPart Required(string content, int order = 0)
    {
        return new QueryPart(content, true, order);
    }

    /// <summary>
    /// 任意クエリパーツ作成
    /// </summary>
    public static QueryPart Optional(string content, int order = 0)
    {
        return new QueryPart(content, !string.IsNullOrWhiteSpace(content), order);
    }

    /// <summary>
    /// 有効性チェック
    /// </summary>
    public bool IsValid => IsRequired && !string.IsNullOrWhiteSpace(Content);

    /// <summary>
    /// 条件付き有効性チェック
    /// </summary>
    public bool IsValidOrOptional => IsValid || (!IsRequired && !string.IsNullOrWhiteSpace(Content));
}
