using Kafka.Ksql.Linq.Core.Abstractions;
using Kafka.Ksql.Linq.Core.Configuration;
using Kafka.Ksql.Linq.Configuration.Messaging;
using System.Collections.Generic;
using System.ComponentModel;

namespace Kafka.Ksql.Linq.Configuration;
public class KsqlDslOptions
{
    /// <summary>
    /// Validation mode (root level setting)
    /// </summary>
    [DefaultValue(ValidationMode.Strict)]
    public ValidationMode ValidationMode { get; init; }

    /// <summary>
    /// Common settings (BootstrapServers, ClientId, etc.)
    /// </summary>
    [DefaultValue(typeof(CommonSection))]
    public CommonSection Common { get; init; } = new();

    /// <summary>
    /// Per-topic settings (manage producer/consumer settings per topic)
    /// </summary>
    [DefaultValue(typeof(Dictionary<string, TopicSection>))]
    public Dictionary<string, TopicSection> Topics { get; init; } = new();

    [DefaultValue(typeof(HeartbeatOptions))]
    public HeartbeatOptions Heartbeat { get; init; } = new();

    /// <summary>
    /// Schema Registry settings
    /// </summary>
    [DefaultValue(typeof(SchemaRegistrySection))]
    public SchemaRegistrySection SchemaRegistry { get; init; } = new();

    /// <summary>
    /// ksqlDB server URL. If omitted, SchemaRegistry.Url's host with port 8088 is used.
    /// </summary>
    public string? KsqlDbUrl { get; init; }

    [DefaultValue(typeof(List<EntityConfiguration>))]
    public List<EntityConfiguration> Entities { get; init; } = new();

    [DefaultValue(typeof(DlqOptions))]
    public DlqOptions DlqOptions { get; init; } = new();

    public string DlqTopicName
    {
        get => DlqOptions.TopicName;
        set => DlqOptions.TopicName = value;
    }

    /// <summary>
    /// Policy when deserialization fails
    /// </summary>
    [DefaultValue(DeserializationErrorPolicy.Skip)]
    public DeserializationErrorPolicy DeserializationErrorPolicy { get; set; }

    /// <summary>
    /// Whether reading from the Final topic is enabled by default
    /// </summary>
    [DefaultValue(false)]
    public bool ReadFromFinalTopicByDefault { get; set; }

    /// <summary>
    /// Optional bar limits per symbol and bar type
    /// </summary>
    [DefaultValue(typeof(BarLimitOptions))]
    public BarLimitOptions BarLimits { get; init; } = new();

    /// <summary>
    /// Global decimal precision applied when mapping decimal properties.
    /// </summary>
    [DefaultValue(18)]
    public int DecimalPrecision { get; init; }

    /// <summary>
    /// Global decimal scale applied when mapping decimal properties.
    /// </summary>
    [DefaultValue(2)]
    public int DecimalScale { get; init; }

    /// <summary>
    /// Optional per-property decimal overrides keyed by entity and property name.
    /// </summary>
    public Dictionary<string, Dictionary<string, DecimalSetting>>? Decimals { get; init; }

    public record DecimalSetting
    {
        public int Precision { get; init; }
        public int Scale   { get; init; }
    }

    /// <summary>
    /// Optional overrides for ksqlDB source object names when generating
    /// CREATE STREAM/TABLE AS SELECT (FROM/JOIN) for query-defined entities.
    /// Key: C# type name (e.g., "Order"). Value: ksqlDB object name (e.g., "ORDERS").
    /// </summary>
    public Dictionary<string, string> SourceNameOverrides { get; init; } = new();
}
