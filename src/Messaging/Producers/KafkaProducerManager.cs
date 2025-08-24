using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry.Serdes;
using Kafka.Ksql.Linq.Configuration;
using Kafka.Ksql.Linq.Core.Abstractions;
using Kafka.Ksql.Linq.Mapping;
using Kafka.Ksql.Linq.Configuration.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ConfluentSchemaRegistry = Confluent.SchemaRegistry;

namespace Kafka.Ksql.Linq.Messaging.Producers;

internal class KafkaProducerManager : IDisposable
{
    private readonly KsqlDslOptions _options;
    private readonly ILogger? _logger;
    private readonly Lazy<ConfluentSchemaRegistry.ISchemaRegistryClient> _schemaRegistryClient;
    private readonly ConcurrentDictionary<Type, ProducerHolder> _producers = new();
    private readonly ConcurrentDictionary<(Type,string), ProducerHolder> _topicProducers = new();
    private bool _disposed;
    private readonly MappingRegistry _mappingRegistry;

    internal sealed class ProducerHolder : IDisposable
    {
        private readonly Func<object?, object?, KafkaMessageContext?, CancellationToken, Task> _sendAsync;
        private readonly Action<TimeSpan> _flush;
        private readonly Action _dispose;
        public string TopicName { get; }

        public ProducerHolder(string topicName,
            Func<object?, object?, KafkaMessageContext?, CancellationToken, Task> sendAsync,
            Action<TimeSpan> flush,
            Action dispose)
        {
            TopicName = topicName;
            _sendAsync = sendAsync;
            _flush = flush;
            _dispose = dispose;
        }

        public Task SendAsync(object? key, object? value, KafkaMessageContext? context, CancellationToken cancellationToken)
            => _sendAsync(key, value, context, cancellationToken);

        public Task FlushAsync(TimeSpan timeout)
        {
            _flush(timeout);
            return Task.CompletedTask;
        }

        public void Dispose() => _dispose();
    }
    public KafkaProducerManager(MappingRegistry mapping,IOptions<KsqlDslOptions> options, ILoggerFactory? loggerFactory = null)
    {
        _mappingRegistry = mapping;
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        DefaultValueBinder.ApplyDefaults(_options);
        _logger = loggerFactory?.CreateLogger<KafkaProducerManager>();
        _schemaRegistryClient = new Lazy<ConfluentSchemaRegistry.ISchemaRegistryClient>(CreateSchemaRegistryClient);
    }

    private EntityModel GetEntityModel<T>() where T : class
    {
        var type = typeof(T);
        return new EntityModel
        {
            EntityType = type,
            TopicName = type.Name.ToLowerInvariant(),
            KeyProperties = Array.Empty<PropertyInfo>(),
            AllProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
        };
    }

    private ProducerConfig BuildProducerConfig(string topicName)
    {
        var section = _options.Topics.TryGetValue(topicName, out var cfg) ? cfg : new TopicSection();
        DefaultValueBinder.ApplyDefaults(section);
        DefaultValueBinder.ApplyDefaults(section.Producer);

        var pc = new ProducerConfig
        {
            BootstrapServers = _options.Common.BootstrapServers,
            ClientId = _options.Common.ClientId,
            Acks = Enum.Parse<Acks>(section.Producer.Acks),
            CompressionType = Enum.Parse<CompressionType>(section.Producer.CompressionType),
            EnableIdempotence = section.Producer.EnableIdempotence,
            MaxInFlight = section.Producer.MaxInFlightRequestsPerConnection,
            LingerMs = section.Producer.LingerMs,
            BatchSize = section.Producer.BatchSize,
            RetryBackoffMs = section.Producer.RetryBackoffMs
        };
        foreach (var kv in section.Producer.AdditionalProperties)
            pc.Set(kv.Key, kv.Value);
        return pc;
    }

    private ConfluentSchemaRegistry.ISchemaRegistryClient CreateSchemaRegistryClient()
    {
        var cfg = new ConfluentSchemaRegistry.SchemaRegistryConfig { Url = _options.SchemaRegistry.Url };
        return new ConfluentSchemaRegistry.CachedSchemaRegistryClient(cfg);
    }


    private ProducerHolder CreateKeyedProducer<TKey,TValue>(string topicName) where TKey : class where TValue : class
    {
        var config = BuildProducerConfig(topicName);
        var prod = new ProducerBuilder<TKey, TValue>(config)
            .SetKeySerializer(new AvroSerializer<TKey>(_schemaRegistryClient.Value).AsSyncOverAsync())
            .SetValueSerializer(new AvroSerializer<TValue>(_schemaRegistryClient.Value).AsSyncOverAsync())
            .Build();
        return new ProducerHolder(
            topicName,
            (k, v, ctx, ct) =>
            {
                var msg = new Message<TKey, TValue> { Key = (TKey?)k!, Value = (TValue?)v! };
                if (ctx?.Headers?.Count > 0)
                    msg.Headers = BuildHeaders(ctx);
                return prod.ProduceAsync(topicName, msg, ct);
            },
            t => prod.Flush(t),
            () => { prod.Flush(System.TimeSpan.FromSeconds(5)); prod.Dispose(); });
    }


    private ProducerHolder CreateProducer(Type keyType, Type valueType, string topicName)
    {
        var method = typeof(KafkaProducerManager).GetMethod(nameof(CreateKeyedProducer), BindingFlags.NonPublic | BindingFlags.Instance)!
            .MakeGenericMethod(keyType, valueType);
        return (ProducerHolder)method.Invoke(this, new object[] { topicName })!;
    }

    private Task<ProducerHolder> GetProducerAsync<TPOCO>(string? topicName = null) where TPOCO : class
    {
        var model = GetEntityModel<TPOCO>();
        var name = (topicName ?? model.TopicName ?? typeof(TPOCO).Name).ToLowerInvariant();
        var mapping = _mappingRegistry.GetMapping(typeof(TPOCO));

        if (topicName == null)
        {
            if (_producers.TryGetValue(typeof(TPOCO), out var existing))
                return Task.FromResult(existing);

            ProducerHolder producer = CreateProducer(mapping.AvroKeyType!, mapping.AvroValueType!, name);

            _producers[typeof(TPOCO)] = producer;
            return Task.FromResult(producer);
        }
        else
        {
            var key = (typeof(TPOCO), name);
            if (_topicProducers.TryGetValue(key, out var existing))
                return Task.FromResult(existing);

            ProducerHolder producer = CreateProducer(mapping.AvroKeyType!, mapping.AvroValueType!, name);

            _topicProducers[key] = producer;
            return Task.FromResult(producer);
        }
    }


    public async Task SendAsync<TPOCO>(string topicName, TPOCO entity, Dictionary<string,string>? headers = null, CancellationToken cancellationToken = default) where TPOCO : class
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        var producer = await GetProducerAsync<TPOCO>(topicName);
        var mapping = _mappingRegistry.GetMapping(typeof(TPOCO));

        object? keyObj;
        object valueObj;
        keyObj = Activator.CreateInstance(mapping.AvroKeyType!)!;
        valueObj = Activator.CreateInstance(mapping.AvroValueType!)!;
        mapping.PopulateAvroKeyValue(entity, keyObj, valueObj);

        var context = new KafkaMessageContext
        {
            MessageId = Guid.NewGuid().ToString(),
            Tags = new Dictionary<string, object>
            {
                ["entity_type"] = typeof(TPOCO).Name,
                ["method"] = "SendAsync"
            }
        };
        if (headers != null)
        {
            foreach (var kvp in headers)
                context.Headers[kvp.Key] = kvp.Value;
        }
        await producer.SendAsync(keyObj, valueObj, context, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync<TPOCO>(TPOCO entity, CancellationToken cancellationToken = default) where TPOCO : class
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        var producer = await GetProducerAsync<TPOCO>();
        var mapping = _mappingRegistry.GetMapping(typeof(TPOCO));

        object? keyObj = Activator.CreateInstance(mapping.AvroKeyType!)!;
        if (mapping.KeyProperties.Length > 0)
        {
            mapping.PopulateAvroKeyValue(entity, keyObj, Activator.CreateInstance(mapping.AvroValueType!)!);
        }

        var context = new KafkaMessageContext
        {
            MessageId = Guid.NewGuid().ToString(),
            Tags = new Dictionary<string, object>
            {
                ["entity_type"] = typeof(TPOCO).Name,
                ["method"] = "DeleteAsync"
            }
        };
        await producer.SendAsync(keyObj, null, context, cancellationToken).ConfigureAwait(false);
    }

    public void Dispose()
    {
        if (_disposed) return;
        foreach (var p in _producers.Values) p.Dispose();
        foreach (var p in _topicProducers.Values) p.Dispose();
        if (_schemaRegistryClient.IsValueCreated)
            _schemaRegistryClient.Value.Dispose();
        _producers.Clear();
        _topicProducers.Clear();
        _disposed = true;
    }

    private static Headers? BuildHeaders(KafkaMessageContext context)
    {
        if (context.Headers == null || context.Headers.Count == 0)
            return null;
        var headers = new Headers();
        foreach (var kvp in context.Headers)
        {
            if (kvp.Value != null)
            {
                var valueString = kvp.Value is bool b ? b.ToString().ToLowerInvariant() : kvp.Value.ToString() ?? string.Empty;
                headers.Add(kvp.Key, System.Text.Encoding.UTF8.GetBytes(valueString));
            }
        }
        return headers;
    }
}
