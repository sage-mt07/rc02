using System;

namespace Kafka.Ksql.Linq.Core.Abstractions;

public interface IErrorHandlingChain<T> where T : class
{
    /// <summary>
    /// Map変換前のエラーハンドリング設定
    /// </summary>
    IMapReadyChain<T> OnError(ErrorAction errorAction);
}

public interface IMapReadyChain<T> where T : class
{
    /// <summary>
    /// Map変換実行（エラーハンドリング適用）
    /// </summary>
    IRetryReadyChain<TResult> Map<TResult>(Func<T, TResult> mapper) where TResult : class;
}

public interface IRetryReadyChain<T> where T : class
{
    /// <summary>
    /// リトライ設定（Mapに適用）
    /// </summary>
    EventSet<T> WithRetry(int retryCount = 3, TimeSpan? retryInterval = null);

    /// <summary>
    /// リトライなしで完了
    /// </summary>
    EventSet<T> Build();
}
