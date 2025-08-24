namespace Kafka.Ksql.Linq.Messaging.Abstractions;

public class TopicPartition
{
    public string Topic { get; set; } = string.Empty;
    public int Partition { get; set; }
}
