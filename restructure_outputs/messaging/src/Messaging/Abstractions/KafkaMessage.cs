namespace Kafka.Ksql.Linq.Messaging.Abstractions;

public class KafkaMessage<TValue, TKey>
    where TValue : class
    where TKey : notnull
{
    public TValue Value { get; set; } = default!;
    public TKey Key { get; set; } = default!;
}
