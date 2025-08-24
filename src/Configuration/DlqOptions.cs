using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Kafka.Ksql.Linq.Configuration;


/// <summary>
/// DLQ設定オプション
/// </summary>
public sealed class DlqOptions
{
    // 機能制御
    public bool Enabled { get; set; } = true;
    public bool EnableForDeserializationError { get; set; } = true;
    public bool EnableForHandlerError { get; set; } = true;
    public double SamplingRate { get; set; } = 1.0;
    public int MaxPerSecond { get; set; } = 0;

    // 例外テキスト整形
    public int ErrorMessageMaxLength { get; set; } = 1024;
    public int StackTraceMaxLength { get; set; } = 2048;
    public bool NormalizeStackTraceWhitespace { get; set; } = true;

    // ヘッダ抽出
    public string[] HeaderAllowList { get; set; } = Array.Empty<string>();
    public int HeaderValueMaxLength { get; set; } = 1024;

    // 例外の除外/含め
    public string[] ExcludedExceptionTypes { get; set; } = new[] { "OperationCanceledException" };
    public string[] IncludedExceptionTypes { get; set; } = Array.Empty<string>();

    // 由来情報
    [DefaultValue("app")]
    public string ApplicationId { get; set; } = string.Empty;
    [DefaultValue("group")]
    public string ConsumerGroup { get; set; } = string.Empty;
    public string Host { get; set; } = Environment.MachineName;

    /// <summary>
    /// 共通DLQトピック名
    /// </summary>
    [DefaultValue("dead-letter-queue")]
    public string TopicName { get; set; } = string.Empty;

    public bool EnableCompression { get; set; } = true;
    public int MaxRetryAttempts { get; set; } = 3;
    public TimeSpan RetryInterval { get; set; } = TimeSpan.FromSeconds(1);
    public Action<DlqMetrics>? MetricsCallback { get; set; }

    /// <summary>
    /// DLQ data retention in milliseconds.
    /// Default: 5000 (5 seconds) - functions as temporary storage.
    /// </summary>
    public long RetentionMs { get; set; } = 5000;

    /// <summary>
    /// Number of partitions for the DLQ topic.
    /// Default: 1 (for observability rather than performance)
    /// </summary>
    public int NumPartitions { get; set; } = 1;

    /// <summary>
    /// Replication factor for the DLQ topic.
    /// Default: 1 (suitable for single-broker environments)
    /// </summary>
    public short ReplicationFactor { get; set; } = 1;

    /// <summary>
    /// Whether to enable automatic DLQ topic creation.
    /// Default: true (avoids fail-fast on missing topic)
    /// </summary>
    public bool EnableAutoCreation { get; set; } = true;

    /// <summary>
    /// Additional DLQ topic settings
    /// e.g., cleanup.policy, segment.ms, max.message.bytes, etc.
    /// </summary>
    [DefaultValue(typeof(Dictionary<string, string>))]
    public Dictionary<string, string> AdditionalConfigs { get; set; } = new();

    /// <summary>
    /// Validate DLQ configuration
    /// </summary>
    public void Validate()
    {
        if (RetentionMs <= 0)
            throw new ArgumentException("DLQ RetentionMs must be positive", nameof(RetentionMs));

        if (NumPartitions <= 0)
            throw new ArgumentException("DLQ NumPartitions must be positive", nameof(NumPartitions));

        if (ReplicationFactor <= 0)
            throw new ArgumentException("DLQ ReplicationFactor must be positive", nameof(ReplicationFactor));
    }
}

/// <summary>
/// DLQメトリクス情報
/// </summary>
public class DlqMetrics
{
    public string TopicName { get; set; } = string.Empty;
    public string OriginalTopic { get; set; } = string.Empty;
    public string ErrorType { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
}

