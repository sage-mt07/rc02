using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kafka.Ksql.Linq.Core.Dlq;

public interface IDlqClient
{
    /// <summary>
    /// DLQ を逐次読み取り、人間可読な RawText を付与して返します。
    /// エラー時もフォールバック（Base64 先頭のみ等）で 1 レコードにまとめます。
    /// </summary>
    IAsyncEnumerable<DlqRecord> ReadAsync(
        DlqReadOptions? options = null,
        CancellationToken ct = default);
}
