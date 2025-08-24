using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kafka.Ksql.Linq.Messaging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using System.Runtime.CompilerServices;

internal class InMemoryKafkaProducer<T> : IKafkaProducer<T> where T : class
{
    private readonly List<T> _messages = new();
    public string TopicName { get; }
    public InMemoryKafkaProducer(string topic) => TopicName = topic;
    public Task<KafkaDeliveryResult> SendAsync(T message, KafkaMessageContext? context = null, CancellationToken cancellationToken = default)
    {
        _messages.Add(message);
        return Task.FromResult(new KafkaDeliveryResult { Topic = TopicName });
    }
    public Task<KafkaBatchDeliveryResult> SendBatchAsync(IEnumerable<T> messages, KafkaMessageContext? context = null, CancellationToken cancellationToken = default)
    {
        _messages.AddRange(messages);
        return Task.FromResult(new KafkaBatchDeliveryResult { Results = messages.Select(_ => new KafkaDeliveryResult { Topic = TopicName }).ToList() });
    }
    public Task FlushAsync(System.TimeSpan timeout) => Task.CompletedTask;
    public void Dispose() { }
}

internal class InMemoryKafkaConsumer<TKey, TValue> : IKafkaConsumer<TKey, TValue>
    where TValue : class
    where TKey : notnull
{
    private readonly Queue<KafkaMessage<TValue, TKey>> _queue;
    public string TopicName { get; }
    public InMemoryKafkaConsumer(string topic, IEnumerable<KafkaMessage<TValue, TKey>> messages)
    {
        TopicName = topic;
        _queue = new Queue<KafkaMessage<TValue, TKey>>(messages);
    }
    public async IAsyncEnumerable<KafkaMessage<TValue, TKey>> ConsumeAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        while (_queue.Count > 0)
        {
            yield return _queue.Dequeue();
            await Task.Yield();
        }
    }
    public Task CommitAsync() => Task.CompletedTask;
    public void Dispose() { }
}

public class ProducerConsumerTests
{
    [Fact]
    public async Task Producer_SendAsync_ReturnsResult()
    {
        var producer = new InMemoryKafkaProducer<string>("test");
        var result = await producer.SendAsync("hello");
        Assert.Equal("test", result.Topic);
    }

    [Fact]
    public async Task Consumer_ConsumeAsync_YieldsMessages()
    {
        var msgs = new[] { new KafkaMessage<string, int> { Value = "v", Key = 1 } };
        var consumer = new InMemoryKafkaConsumer<int, string>("test", msgs);
        var received = new List<KafkaMessage<string, int>>();
        await foreach (var m in consumer.ConsumeAsync())
        {
            received.Add(m);
        }
        Assert.Single(received);
        Assert.Equal("v", received[0].Value);
        Assert.Equal(1, received[0].Key);
    }

    [Fact]
    public async Task Producer_SendAsync_InvokesMock()
    {
        var mock = new Mock<IKafkaProducer<string>>();
        mock.Setup(p => p.SendAsync("data", null, default))
            .ReturnsAsync(new KafkaDeliveryResult { Topic = "mock" });

        var result = await mock.Object.SendAsync("data");

        Assert.Equal("mock", result.Topic);
        mock.Verify(p => p.SendAsync("data", null, default), Times.Once);
    }

    [Fact]
    public async Task Services_CanResolveProducerAndConsumer()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IKafkaProducer<string>>(new InMemoryKafkaProducer<string>("svc"));
        services.AddSingleton<IKafkaConsumer<int, string>>(new InMemoryKafkaConsumer<int, string>("svc", Enumerable.Empty<KafkaMessage<string, int>>()));

        using var provider = services.BuildServiceProvider();
        var producer = provider.GetRequiredService<IKafkaProducer<string>>();
        var consumer = provider.GetRequiredService<IKafkaConsumer<int, string>>();

        var sendResult = await producer.SendAsync("value");
        Assert.Equal("svc", sendResult.Topic);
        var messages = new List<KafkaMessage<string, int>>();
        await foreach (var m in consumer.ConsumeAsync())
            messages.Add(m);
        Assert.Empty(messages);
    }
}
