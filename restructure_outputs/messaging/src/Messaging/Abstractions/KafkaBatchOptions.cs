namespace Kafka.Ksql.Linq.Messaging.Abstractions;

using System;

public class KafkaBatchOptions
{
    public int MaxBatchSize { get; set; } = 1;
    public TimeSpan MaxWaitTime { get; set; } = TimeSpan.FromSeconds(1);
}
