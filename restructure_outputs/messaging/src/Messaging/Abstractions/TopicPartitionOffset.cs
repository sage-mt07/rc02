namespace Kafka.Ksql.Linq.Messaging.Abstractions;

public class TopicPartitionOffset : TopicPartition
{
    public long Offset { get; set; }
}
