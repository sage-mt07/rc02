using Kafka.Ksql.Linq.Core.Abstractions;
using System.Threading.Tasks;

namespace Kafka.Ksql.Linq.Core.Abstractions;

public interface IErrorSink
{
    /// <summary>
    /// エラーレコードを処理（DLQ送信等）
    /// </summary>
    /// <param name="errorContext">エラーコンテキスト情報</param>
    /// <param name="messageContext">Kafkaメッセージコンテキスト</param>
    /// <returns>処理完了タスク</returns>
    Task HandleErrorAsync(ErrorContext errorContext, KafkaMessageContext messageContext);

    /// <summary>
    /// エラーレコードを処理（オーバーロード - メッセージコンテキストなし）
    /// </summary>
    /// <param name="errorContext">エラーコンテキスト情報</param>
    /// <returns>処理完了タスク</returns>
    Task HandleErrorAsync(ErrorContext errorContext);

    /// <summary>
    /// エラーシンクの初期化
    /// </summary>
    /// <returns>初期化完了タスク</returns>
    Task InitializeAsync();

    /// <summary>
    /// エラーシンクのクリーンアップ
    /// </summary>
    /// <returns>クリーンアップ完了タスク</returns>
    Task CleanupAsync();

    /// <summary>
    /// エラーシンクが利用可能かどうか
    /// </summary>
    bool IsAvailable { get; }
}
