namespace Kafka.Ksql.Linq.Core.Abstractions;

public enum ErrorAction
{
    /// <summary>
    /// エラーレコードをスキップして処理継続
    /// </summary>
    Skip,

    /// <summary>
    /// 指定回数リトライ
    /// </summary>
    Retry,

    /// <summary>
    /// Dead Letter Queueに送信
    /// </summary>
    DLQ
}
