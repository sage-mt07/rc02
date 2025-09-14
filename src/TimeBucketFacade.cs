using Kafka.Ksql.Linq.Runtime;

namespace Kafka.Ksql.Linq;

/// <summary>
/// Facade for TimeBucket APIs to avoid exposing the Runtime namespace
/// in call sites. Forwards to Kafka.Ksql.Linq.Runtime.TimeBucket.
/// </summary>
public static class TimeBucket
{
    public static Runtime.TimeBucket<T> Get<T>(ITimeBucketContext context, Period period) where T : class
        => Runtime.TimeBucket.Get<T>(context, period);

    public static TimeBucketWriter<T> Set<T>(ITimeBucketWriteContext context, Period period) where T : class
        => Runtime.TimeBucket.Set<T>(context, period);
}

