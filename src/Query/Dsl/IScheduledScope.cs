using System;
using System.Linq.Expressions;

namespace Kafka.Ksql.Linq.Query.Dsl;

public interface IScheduledScope<T>
{
    KsqlQueryable<T> Tumbling(
        Expression<Func<T, DateTime>> time,
        int[]? minutes = null,
        int[]? hours = null,
        int[]? days = null,
        int[]? months = null,
        DayOfWeek? week = null,
        TimeSpan? grace = null);
}
