namespace Kafka.Ksql.Linq.Messaging.Abstractions;

using System.Collections.Generic;

public class KafkaBatchDeliveryResult
{
    public List<KafkaDeliveryResult> Results { get; set; } = new();
}
