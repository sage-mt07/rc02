using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Kafka.Ksql.Linq.Configuration;
using Kafka.Ksql.Linq.Core.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Kafka.Ksql.Linq.Infrastructure.Admin;

internal class KafkaAdminService : IDisposable
{
    private readonly IAdminClient _adminClient;
    private readonly ILogger<KafkaAdminService>? _logger;
    private readonly KsqlDslOptions _options;
    private bool _disposed = false;

    public KafkaAdminService(IOptions<KsqlDslOptions> options, ILoggerFactory? loggerFactory = null)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = loggerFactory?.CreateLogger<KafkaAdminService>();

        var adminConfig = CreateAdminConfig();
        _adminClient = new AdminClientBuilder(adminConfig).Build();

        _logger?.LogDebug("KafkaAdminService initialized with BootstrapServers: {BootstrapServers}",
            adminConfig.BootstrapServers);
    }

    /// <summary>
    /// Check for the DLQ topic and create it automatically if missing.
    /// Timing: called at the end of KafkaContext.InitializeWithSchemaRegistration().
    /// </summary>
    public async Task EnsureDlqTopicExistsAsync(CancellationToken cancellationToken = default)
    {
        var dlqTopicName = _options.DlqTopicName;

        try
        {
            // 1. Check if the topic already exists
            if (TopicExists(dlqTopicName, cancellationToken))
            {
                _logger?.LogDebug("DLQ topic already exists: {DlqTopicName}", dlqTopicName);
                return;
            }

            // 2. Create the DLQ topic
            await CreateDlqTopicAsync(dlqTopicName, cancellationToken);
            _logger?.LogInformation("DLQ topic created successfully: {DlqTopicName}", dlqTopicName);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to ensure DLQ topic exists: {DlqTopicName}", dlqTopicName);
            throw new InvalidOperationException(
                $"FATAL: Cannot ensure DLQ topic '{dlqTopicName}' exists. " +
                "DLQ functionality will be unavailable.", ex);
        }
    }

    internal async Task EnsureTopicExistsAsync(string topicName, CancellationToken cancellationToken = default)
    {
        if (TopicExists(topicName, cancellationToken))
        {
            if (topicName == _options.Heartbeat.Topic)
                WarnIfMismatchWithRate1m(topicName);
            _logger?.LogDebug("Topic already exists: {TopicName}", topicName);
            return;
        }

        var spec = new TopicSpecification
        {
            Name = topicName,
            NumPartitions = 1,
            ReplicationFactor = 1
        };
        if (topicName == _options.Heartbeat.Topic)
            ApplyRate1mSpec(spec);
        // appsettings keys are looked up by the resolved topic name
        if (_options.Topics.TryGetValue(topicName, out var sec) && sec?.Creation != null)
        {
            var c = sec.Creation;
            if (c.NumPartitions > 0)
                spec.NumPartitions = c.NumPartitions;
            if (c.ReplicationFactor > 0)
                spec.ReplicationFactor = c.ReplicationFactor;
            if (c.Configs.Count > 0)
                spec.Configs = new Dictionary<string, string>(c.Configs);
        }

        // Adjust replication factor to available brokers (dev/local single-broker clusters)
        try
        {
            var md = _adminClient.GetMetadata(TimeSpan.FromSeconds(5));
            var brokers = md?.Brokers?.Count ?? 0;
            if (brokers > 0 && spec.ReplicationFactor > brokers)
            {
                _logger?.LogWarning(
                    "Requested replication factor {RF} exceeds broker count {Brokers} for {Topic}. Using {AdjRF}.",
                    spec.ReplicationFactor, brokers, topicName, (short)1);
                spec.ReplicationFactor = 1;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Failed to get cluster metadata while adjusting RF for {Topic}.", topicName);
        }

        try
        {
            await _adminClient.CreateTopicsAsync(new[] { spec }, new CreateTopicsOptions
            {
                RequestTimeout = TimeSpan.FromSeconds(30)
            });

            _logger?.LogInformation("Topic created: {TopicName}", topicName);
        }
        catch (CreateTopicsException ex)
        {
            var result = ex.Results.FirstOrDefault(r => r.Topic == topicName);
            if (result?.Error.Code == ErrorCode.TopicAlreadyExists)
            {
                _logger?.LogDebug("Topic already exists (race): {TopicName}", topicName);
                return;
            }
            // Retry once with RF=1 if RF invalid
            if (result?.Error.Code == ErrorCode.InvalidReplicationFactor ||
                (result?.Error.Reason?.IndexOf("Replication factor", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                try
                {
                    spec.ReplicationFactor = 1;
                    _logger?.LogWarning("Retrying creation of {Topic} with ReplicationFactor=1 due to invalid RF.", topicName);
                    await _adminClient.CreateTopicsAsync(new[] { spec }, new CreateTopicsOptions { RequestTimeout = TimeSpan.FromSeconds(30) });
                    _logger?.LogInformation("Topic created (RF=1): {Topic}", topicName);
                    return;
                }
                catch (Exception retryEx)
                {
                    _logger?.LogError(retryEx, "Retry with RF=1 failed for topic {Topic}", topicName);
                }
            }
            throw new InvalidOperationException($"Failed to create topic '{topicName}': {result?.Error.Reason ?? "Unknown"}", ex);
        }
    }

    /// <summary>
    /// Check whether the topic exists
    /// </summary>
    private bool TopicExists(string topicName, CancellationToken cancellationToken)
    {
        try
        {
            var metadata = _adminClient.GetMetadata(TimeSpan.FromSeconds(10));
            return metadata.Topics.Any(t => t.Topic == topicName && !t.Error.IsError);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to check topic existence: {TopicName}", topicName);
            return false;
        }
    }

    private void ApplyRate1mSpec(TopicSpecification spec)
    {
        var baseTopic = _options.Topics.FirstOrDefault(kv => kv.Key.StartsWith("rate_1m_") && kv.Value?.Creation != null);
        var c = baseTopic.Value?.Creation;
        if (c == null) return;
        spec.NumPartitions = c.NumPartitions;
        spec.ReplicationFactor = c.ReplicationFactor;
    }

    private void WarnIfMismatchWithRate1m(string topicName)
    {
        var baseTopic = _options.Topics.FirstOrDefault(kv => kv.Key.StartsWith("rate_1m_") && kv.Value?.Creation != null);
        var c = baseTopic.Value?.Creation;
        if (c == null) return;
        try
        {
            var meta = _adminClient.GetMetadata(topicName, TimeSpan.FromSeconds(10)).Topics.FirstOrDefault();
            if (meta == null) return;
            var partitions = meta.Partitions.Count;
            short repl = meta.Partitions.Count > 0 ? (short)meta.Partitions[0].Replicas.Length : (short)0;
            if (partitions != c.NumPartitions || repl != c.ReplicationFactor)
                _logger?.LogWarning("hb_1m topic differs from rate_1m_* partitions or replication; cannot adjust.");
        }
        catch { }
    }

    /// <summary>
    /// Create a DB topic; no-op if it already exists
    /// </summary>
    public async Task CreateDbTopicAsync(string topicName, int partitions, short replicationFactor)
    {
        if (string.IsNullOrWhiteSpace(topicName))
            throw new ArgumentException("Topic name is required", nameof(topicName));
        if (partitions <= 0)
            throw new ArgumentException("partitions must be > 0", nameof(partitions));
        if (replicationFactor <= 0)
            throw new ArgumentException("replicationFactor must be > 0", nameof(replicationFactor));

        if (TopicExists(topicName, CancellationToken.None))
        {
            _logger?.LogDebug("DB topic already exists: {Topic}", topicName);
            return;
        }

        // Adjust replication factor to available brokers
        short effectiveRf = replicationFactor;
        try
        {
            var md = _adminClient.GetMetadata(TimeSpan.FromSeconds(5));
            var brokers = md?.Brokers?.Count ?? 0;
            if (brokers > 0 && replicationFactor > brokers)
            {
                _logger?.LogWarning("Requested replication factor {RF} exceeds broker count {Brokers} for {Topic}. Using {AdjRF}.", replicationFactor, brokers, topicName, (short)1);
                effectiveRf = 1;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Failed to get cluster metadata while adjusting DB RF for {Topic}.", topicName);
        }

        var spec = new TopicSpecification
        {
            Name = topicName,
            NumPartitions = partitions,
            ReplicationFactor = effectiveRf
        };

        try
        {
            _logger?.LogDebug("Creating DB topic {Topic} (partitions={Partitions}, rf={RF})", topicName, spec.NumPartitions, spec.ReplicationFactor);
            await _adminClient.CreateTopicsAsync(new[] { spec }, new CreateTopicsOptions { RequestTimeout = TimeSpan.FromSeconds(30) });
            _logger?.LogInformation("DB topic created: {Topic}", topicName);
        }
        catch (CreateTopicsException ex)
        {
            var result = ex.Results.FirstOrDefault(r => r.Topic == topicName);
            if (result?.Error.Code == ErrorCode.TopicAlreadyExists)
            {
                _logger?.LogDebug("DB topic already exists (race): {Topic}", topicName);
                return;
            }
            if (result?.Error.Code == ErrorCode.Local_TimedOut || (result?.Error.Reason?.IndexOf("controller", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                _logger?.LogWarning("CreateTopics timeout or controller not ready for {Topic}: {Reason}", topicName, result?.Error.Reason);
            }
            // Retry once with RF=1 if RF invalid
            if (result?.Error.Code == ErrorCode.InvalidReplicationFactor ||
                (result?.Error.Reason?.IndexOf("Replication factor", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                try
                {
                    spec.ReplicationFactor = 1;
                    _logger?.LogWarning("Retrying DB topic creation for {Topic} with ReplicationFactor=1 due to invalid RF.", topicName);
                    await _adminClient.CreateTopicsAsync(new[] { spec }, new CreateTopicsOptions { RequestTimeout = TimeSpan.FromSeconds(30) });
                    _logger?.LogInformation("DB topic created (RF=1): {Topic}", topicName);
                    return;
                }
                catch (Exception retryEx)
                {
                    _logger?.LogError(retryEx, "Retry with RF=1 failed for DB topic {Topic}", topicName);
                }
            }
            throw new InvalidOperationException($"Failed to create DB topic '{topicName}': {result?.Error.Reason ?? ex.Message}", ex);
        }
    }

    /// <summary>
    /// Create the DLQ topic.
    /// Settings are dynamically applied from DlqOptions.
    /// </summary>
    private async Task CreateDlqTopicAsync(string topicName, CancellationToken cancellationToken)
    {
        var dlqConfig = _options.DlqOptions;

        // Skip if automatic DLQ creation is disabled
        if (!dlqConfig.EnableAutoCreation)
        {
            _logger?.LogInformation("Skipping DLQ topic creation because auto-creation is disabled: {TopicName}", topicName);
            return;
        }

        var configs = new Dictionary<string, string>
        {
            ["retention.ms"] = dlqConfig.RetentionMs.ToString()
        };

        // Merge additional settings
        foreach (var kvp in dlqConfig.AdditionalConfigs)
        {
            configs[kvp.Key] = kvp.Value;
        }

        var topicSpec = new TopicSpecification
        {
            Name = topicName,
            NumPartitions = dlqConfig.NumPartitions,
            ReplicationFactor = dlqConfig.ReplicationFactor,
            Configs = configs
        };

        try
        {
            await _adminClient.CreateTopicsAsync(
                new[] { topicSpec },
                new CreateTopicsOptions { RequestTimeout = TimeSpan.FromSeconds(30) });

            _logger?.LogInformation("DLQ topic created: {TopicName} with {RetentionMs}ms retention, {Partitions} partitions",
                topicName, dlqConfig.RetentionMs, dlqConfig.NumPartitions);
        }
        catch (CreateTopicsException ex)
        {
            // Check each result
            var result = ex.Results.FirstOrDefault(r => r.Topic == topicName);
            if (result?.Error.Code == ErrorCode.TopicAlreadyExists)
            {
                _logger?.LogDebug("DLQ topic already exists (race condition): {TopicName}", topicName);
                return; // Another instance created it first
            }

            throw new InvalidOperationException(
                $"Failed to create DLQ topic '{topicName}': {result?.Error.Reason ?? "Unknown error"}", ex);
        }
    }

    /// <summary>
    /// Verify Kafka connectivity (used during KafkaContext initialization)
    /// </summary>
    public void ValidateKafkaConnectivity(CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = _adminClient.GetMetadata(TimeSpan.FromSeconds(10));
            if (metadata == null || metadata.Brokers.Count == 0)
            {
                throw new InvalidOperationException("No Kafka brokers found in metadata");
            }

            _logger?.LogDebug("Kafka connectivity validated: {BrokerCount} brokers available",
                metadata.Brokers.Count);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "FATAL: Cannot connect to Kafka cluster. Verify bootstrap servers and network connectivity.", ex);
        }
    }

    /// <summary>
    /// Build the AdminClient configuration
    /// </summary>
    private AdminClientConfig CreateAdminConfig()
    {
        var config = new AdminClientConfig
        {
            BootstrapServers = _options.Common.BootstrapServers,
            ClientId = $"{_options.Common.ClientId}-admin",
            //  RequestTimeoutMs = _options.Common.RequestTimeoutMs,
            MetadataMaxAgeMs = _options.Common.MetadataMaxAgeMs
        };

        // Security settings
        if (_options.Common.SecurityProtocol != SecurityProtocol.Plaintext)
        {
            config.SecurityProtocol = _options.Common.SecurityProtocol;

            if (_options.Common.SaslMechanism.HasValue)
            {
                config.SaslMechanism = _options.Common.SaslMechanism.Value;
                config.SaslUsername = _options.Common.SaslUsername;
                config.SaslPassword = _options.Common.SaslPassword;
            }

            if (!string.IsNullOrEmpty(_options.Common.SslCaLocation))
            {
                config.SslCaLocation = _options.Common.SslCaLocation;
                config.SslCertificateLocation = _options.Common.SslCertificateLocation;
                config.SslKeyLocation = _options.Common.SslKeyLocation;
                config.SslKeyPassword = _options.Common.SslKeyPassword;
            }
        }

        // Apply additional settings
        foreach (var kvp in _options.Common.AdditionalProperties)
        {
            config.Set(kvp.Key, kvp.Value);
        }

        return config;
    }

    /// <summary>
    /// Release resources
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            try
            {
                _adminClient?.Dispose();
                _logger?.LogDebug("KafkaAdminService disposed");
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error disposing KafkaAdminService");
            }

            _disposed = true;
        }
    }
}
