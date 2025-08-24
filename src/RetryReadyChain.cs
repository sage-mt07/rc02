using Kafka.Ksql.Linq.Core.Abstractions;
using System;

namespace Kafka.Ksql.Linq;

internal class RetryReadyChain<T> : IRetryReadyChain<T> where T : class
{
    private readonly EventSet<T> _eventSet;

    public RetryReadyChain(EventSet<T> eventSet)
    {
        _eventSet = eventSet ?? throw new ArgumentNullException(nameof(eventSet));
    }

    public EventSet<T> WithRetry(int retryCount = 3, TimeSpan? retryInterval = null)
    {
        if (retryCount < 0)
            throw new ArgumentException("Retry count cannot be negative", nameof(retryCount));

        var retryPolicy = new ErrorHandlingPolicy
        {
            Action = ErrorAction.Retry,
            RetryCount = retryCount,
            RetryInterval = retryInterval ?? TimeSpan.FromSeconds(1)
        };

        return _eventSet.WithErrorPolicy(retryPolicy);
    }

    public EventSet<T> Build()
    {
        return _eventSet;
    }
}
