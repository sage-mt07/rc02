using Kafka.Ksql.Linq.Query.Abstractions;
using System;
using System.Reflection;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace Kafka.Ksql.Linq.Core.Abstractions;

public class EntityModel
{
    public Type EntityType { get; set; } = null!;

    public string? TopicName { get; set; }

    /// <summary>
    /// LINQ expression used for query-based entity definitions.
    /// </summary>
    public LambdaExpression? QueryExpression { get; set; }

    /// <summary>
    /// New DSL query model for CREATE statements.
    /// </summary>
    public Kafka.Ksql.Linq.Query.Dsl.KsqlQueryModel? QueryModel { get; set; }

    /// <summary>
    /// Number of partitions for the backing Kafka topic.
    /// </summary>
    public int Partitions { get; set; } = 1;

    /// <summary>
    /// Replication factor for the backing Kafka topic.
    /// </summary>
    public short ReplicationFactor { get; set; } = 1;

    /// <summary>
    /// Schema Registry ID for the key schema.
    /// </summary>
    public int? KeySchemaId { get; set; }

    /// <summary>
    /// Schema Registry ID for the value schema.
    /// </summary>
    public int? ValueSchemaId { get; set; }

    public PropertyInfo[] KeyProperties { get; set; } = Array.Empty<PropertyInfo>();

    public PropertyInfo[] AllProperties { get; set; } = Array.Empty<PropertyInfo>();

    /// <summary>
    /// Optional selector expression identifying the bar timestamp used for
    /// ordering or limiting operations. This is automatically populated when
    /// <c>Select&lt;TResult&gt;()</c> is used with Window DSL and a property assignment
    /// from <c>WindowGrouping.BarStart</c> is detected.
    /// </summary>
    public LambdaExpression? BarTimeSelector { get; set; }

    /// <summary>
    /// Indicates whether this entity is used for reading, writing, or both.
    /// </summary>
    public EntityAccessMode AccessMode { get; set; } = EntityAccessMode.ReadWrite;

    public ValidationResult? ValidationResult { get; set; }

    public bool IsValid => ValidationResult?.IsValid ?? false;
    public StreamTableType StreamTableType
    {
        get
        {
            if (_explicitStreamTableType.HasValue)
                return _explicitStreamTableType.Value;

            if (HasKeys())
                return StreamTableType.Table;

            return StreamTableType.Stream;
        }
    }
    /// <summary>
    /// Stream/Table型の明示的設定
    /// </summary>
    /// <param name="streamTableType">設定する型</param>
    public void SetStreamTableType(StreamTableType streamTableType)
    {
        _explicitStreamTableType = streamTableType;
    }

    private StreamTableType? _explicitStreamTableType;

    /// <summary>
    /// 明示的に設定されたStream/Table型を取得
    /// </summary>
    public StreamTableType GetExplicitStreamTableType()
    {
        return _explicitStreamTableType ?? StreamTableType;
    }

    /// <summary>
    /// キープロパティの有無を確認
    /// 設計理由：Stream/Table判定に必要、CoreExtensions.HasKeys()と同等機能
    /// </summary>
    public bool HasKeys()
    {
        return KeyProperties != null && KeyProperties.Length > 0;
    }

    /// <summary>
    /// 複合キーかどうかを確認
    /// </summary>
    public bool IsCompositeKey()
    {
        return KeyProperties != null && KeyProperties.Length > 1;
    }
    /// <summary>
    /// 処理エラー発生時のアクション
    /// </summary>
    public ErrorAction ErrorAction { get; set; } = ErrorAction.Skip;

    /// <summary>
    /// デシリアライズ失敗時のポリシー
    /// </summary>
    public DeserializationErrorPolicy DeserializationErrorPolicy { get; set; } = DeserializationErrorPolicy.Skip;

    /// <summary>
    /// RocksDB キャッシュ利用フラグ
    /// </summary>
    public bool EnableCache { get; set; } = true;

    /// <summary>
    /// Optional consumer group id specified via DSL or configuration.
    /// </summary>
    public string? GroupId { get; set; }

    /// <summary>
    /// Additional extensible settings. Designed for future DSL/config items.
    /// </summary>
    public Dictionary<string, object> AdditionalSettings { get; } = new();

}
