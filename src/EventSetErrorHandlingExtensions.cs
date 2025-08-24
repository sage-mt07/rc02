using Kafka.Ksql.Linq.Core.Abstractions;
using Kafka.Ksql.Linq.Messaging;
using System;

namespace Kafka.Ksql.Linq;

public static class EventSetErrorHandlingExtensions
{
    public static IErrorHandlingChain<T> StartErrorHandling<T>(this EventSet<T> eventSet) where T : class
    {
        return new ErrorHandlingChain<T>(eventSet);
    }

    public static EventSet<T> OnError<T>(this EventSet<T> eventSet, ErrorAction errorAction) where T : class
    {
        if (typeof(T) == typeof(Messaging.DlqEnvelope) && errorAction == ErrorAction.DLQ)
            throw new InvalidOperationException("DLQストリームでOnError(DLQ)は利用できません（無限ループ防止のため）");

        var policy = new ErrorHandlingPolicy
        {
            Action = errorAction
        };

        return eventSet.WithErrorPolicy(policy);
    }
}
