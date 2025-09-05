using System;

namespace Kafka.Ksql.Linq.Core.Abstractions;

public interface IErrorHandlingChain<T> where T : class
{
    /// <summary>
    /// Configure error handling prior to Map transformation.
    /// </summary>
    IMapReadyChain<T> OnError(ErrorAction errorAction);
}

public interface IMapReadyChain<T> where T : class
{
    /// <summary>
    /// Execute Map transformation (with error handling applied).
    /// </summary>
    IRetryReadyChain<TResult> Map<TResult>(Func<T, TResult> mapper) where TResult : class;
}

public interface IRetryReadyChain<T> where T : class
{
    /// <summary>
    /// Configure retry (applied to Map).
    /// </summary>
    EventSet<T> WithRetry(int retryCount = 3, TimeSpan? retryInterval = null);

    /// <summary>
    /// Complete without retry.
    /// </summary>
    EventSet<T> Build();
}
