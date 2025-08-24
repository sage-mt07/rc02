using Kafka.Ksql.Linq.Core.Abstractions;
using System;

namespace Kafka.Ksql.Linq;

internal class MapReadyChain<T> : IMapReadyChain<T> where T : class
{
    private readonly EventSet<T> _eventSet;
    private readonly ErrorAction _errorAction;

    public MapReadyChain(EventSet<T> eventSet, ErrorAction errorAction)
    {
        _eventSet = eventSet ?? throw new ArgumentNullException(nameof(eventSet));
        _errorAction = errorAction;
    }

    public IRetryReadyChain<TResult> Map<TResult>(Func<T, TResult> mapper) where TResult : class
    {
        if (mapper == null)
            throw new ArgumentNullException(nameof(mapper));

        var eventSetWithErrorPolicy = _eventSet.OnError(_errorAction);

        var mappedEventSet = ApplyMapTransformation(eventSetWithErrorPolicy, mapper);

        if (mappedEventSet == null)
            throw new InvalidOperationException("Map transformation failed to create mapped EventSet");

        return new RetryReadyChain<TResult>(mappedEventSet);
    }

    private EventSet<TResult> ApplyMapTransformation<TResult>(EventSet<T> eventSet, Func<T, TResult> mapper)
        where TResult : class
    {
        try
        {
            var mappedItems = new System.Collections.Generic.List<TResult>();
            return new MappedEventSet<TResult>(
                mappedItems,
                eventSet.GetContext(),
                eventSet.GetEntityModel());
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Map transformation failed from {typeof(T).Name} to {typeof(TResult).Name}", ex);
        }
    }
}
