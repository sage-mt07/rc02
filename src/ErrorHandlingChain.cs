using Kafka.Ksql.Linq.Core.Abstractions;
using System;

namespace Kafka.Ksql.Linq;

internal class ErrorHandlingChain<T> : IErrorHandlingChain<T> where T : class
{
    private readonly EventSet<T> _eventSet;

    public ErrorHandlingChain(EventSet<T> eventSet)
    {
        _eventSet = eventSet ?? throw new ArgumentNullException(nameof(eventSet));
    }

    public IMapReadyChain<T> OnError(ErrorAction errorAction)
    {
        return new MapReadyChain<T>(_eventSet, errorAction);
    }
}
