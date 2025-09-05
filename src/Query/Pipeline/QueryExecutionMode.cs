namespace Kafka.Ksql.Linq.Query.Pipeline;
/// <summary>
/// クエリ実行モード
/// Rationale: distinguish Pull queries (one-shot) from Push queries (streaming)
/// </summary>
public enum QueryExecutionMode
{
    /// <summary>
    /// Execution mode not explicitly specified.
    /// </summary>
    Unspecified,
    /// <summary>
    /// Pull Query - 一回限りのクエリ実行
    /// </summary>
    PullQuery,

    /// <summary>
    /// Push Query - 継続的なストリーミングクエリ
    /// </summary>
    PushQuery
}
