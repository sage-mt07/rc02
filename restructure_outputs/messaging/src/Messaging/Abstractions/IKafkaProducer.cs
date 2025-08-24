namespace Kafka.Ksql.Linq.Messaging.Abstractions;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public interface IKafkaProducer<T> : IDisposable where T : class
{
    Task<KafkaDeliveryResult> SendAsync(T message, KafkaMessageContext? context = null, CancellationToken cancellationToken = default);
    Task<KafkaBatchDeliveryResult> SendBatchAsync(IEnumerable<T> messages, KafkaMessageContext? context = null, CancellationToken cancellationToken = default);
    Task FlushAsync(TimeSpan timeout);
    string TopicName { get; }
}
