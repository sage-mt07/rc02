using Kafka.Ksql.Linq.Core.Abstractions;
using System;

namespace Kafka.Ksql.Linq;

/// <summary>
/// Extensions to enable error handling DSL on IEntitySet when the underlying
/// implementation derives from EventSet.
/// </summary>
public static class EntitySetErrorHandlingExtensions
{
    /// <summary>
    /// Applies error handling policy to the entity set.
    /// </summary>
    public static EventSet<T> OnError<T>(this IEntitySet<T> entitySet, ErrorAction errorAction) where T : class
    {
        if (entitySet is EventSet<T> eventSet)
        {
            return eventSet.OnError(errorAction);
        }
        throw new InvalidOperationException("OnError is only supported on EventSet-based entity sets.");
    }

    /// <summary>
    /// Starts the error handling chain when the entity set is EventSet based.
    /// </summary>
    public static IErrorHandlingChain<T> StartErrorHandling<T>(this IEntitySet<T> entitySet) where T : class
    {
        if (entitySet is EventSet<T> eventSet)
        {
            return eventSet.StartErrorHandling();
        }
        throw new InvalidOperationException("StartErrorHandling is only supported on EventSet-based entity sets.");
    }
}

