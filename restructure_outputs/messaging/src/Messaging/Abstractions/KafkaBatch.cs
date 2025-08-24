namespace Kafka.Ksql.Linq.Messaging.Abstractions;

using System.Collections.Generic;

public class KafkaBatch<TValue, TKey>
    where TValue : class
    where TKey : notnull
{
    public List<KafkaMessage<TValue, TKey>> Messages { get; set; } = new();
}
