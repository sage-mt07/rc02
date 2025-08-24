using Kafka.Ksql.Linq.Core.Modeling;
using Kafka.Ksql.Linq.Core.Abstractions;
using System;

// 拡張メソッド IsManaged を定義したクラスを利用する場合、
// このファイルと同じ namespace に ManagedTopicExtensions が存在する。
namespace Samples.TopicFluentApiExtension;

// IsManaged を利用したトピック設定とエラーハンドリング例
public static class Example2_ManagedMode
{
    [KsqlTopic("logs", PartitionCount = 6, ReplicationFactor = 3)]
    private class LogEntry
    {
        [KsqlKey(Order = 0)]
        public string Id { get; set; } = string.Empty;
    }

    public static void Configure(ModelBuilder builder)
    {
        try
        {
            builder.Entity<LogEntry>()
                .AsTable("logs")
                // 既存トピックとの衝突を検出して自動管理モードへ
                .IsManaged(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Topic setup error: {ex.Message}");
            // 必要に応じてロギングやリトライを実装
        }
    }

    /// <summary>
    /// Initializes a model builder and prints whether the managed flag was set.
    /// </summary>
    public static void Run()
    {
        // ModelBuilder is an internal type so we create it via reflection
        var mbType = typeof(IModelBuilder).Assembly.GetType("Kafka.Ksql.Linq.Core.Modeling.ModelBuilder")!;
        dynamic modelBuilder = Activator.CreateInstance(mbType)!;
        Configure(modelBuilder);
        bool managed = modelBuilder.Entity<LogEntry>().GetIsManaged();
        Console.WriteLine($"IsManaged: {managed}");
    }
}
