using Kafka.Ksql.Linq;
using Kafka.Ksql.Linq.Configuration;
using Kafka.Ksql.Linq.Core.Abstractions;
using Kafka.Ksql.Linq.Core.Attributes;
using Kafka.Ksql.Linq.Core.Modeling;
using Kafka.Ksql.Linq.Query.Dsl;
using Kafka.Ksql.Linq.Runtime;
using Microsoft.Extensions.Logging;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Kafka.Ksql.Linq.Tests.Integration;

/// <summary>
/// TimeBucket を使用してデータのインポート後、
/// Tumbling を使用したクエリ定義により生成された足データを抽出する物理テスト。
/// 実行にはローカルの Kafka/SchemaRegistry/ksqlDB 環境が必要です。
/// </summary>
public class TimeBucketImportTumblingTests
{
    [KsqlTopic("ticks")]
    private class Tick
    {
        [KsqlKey(1)] public string Broker { get; set; } = string.Empty;
        [KsqlKey(2)] public string Symbol { get; set; } = string.Empty;
        [KsqlTimestamp] public DateTime TimestampUtc { get; set; }
        public decimal Bid { get; set; }
    }

    private class Bar
    {
        [KsqlKey(1)] public string Broker { get; set; } = string.Empty;
        [KsqlKey(2)] public string Symbol { get; set; } = string.Empty;
        [KsqlKey(3)] public DateTime BucketStart { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
    }

    private sealed class TestContext : KsqlContext
    {
        private static readonly ILoggerFactory _loggerFactory = LoggerFactory.Create(b => b.AddConsole());
        public TestContext() : base(new KsqlDslOptions
        {
            // Resolve endpoints from environment when available (Docker runner),
            // fallback to host defaults when running locally
            Common = new CommonSection
            {
                BootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "127.0.0.1:39092"
            },
            SchemaRegistry = new Kafka.Ksql.Linq.Core.Configuration.SchemaRegistrySection
            {
                Url = Environment.GetEnvironmentVariable("SCHEMA_REGISTRY_URL") ?? "http://127.0.0.1:18081"
            },
            KsqlDbUrl = Environment.GetEnvironmentVariable("KSQLDB_URL") ?? "http://127.0.0.1:18088",
            Topics =
            {
                ["ticks"] = new Kafka.Ksql.Linq.Configuration.Messaging.TopicSection
                {
                    Producer = new Kafka.Ksql.Linq.Configuration.Messaging.ProducerSection
                    {
                        LingerMs = 0,
                        BatchNumMessages = 1
                    }
                }
            }
        }, _loggerFactory) { }

        public EventSet<Tick> Ticks { get; set; } = null!;

        protected override void OnModelCreating(IModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Bar>()
                .ToQuery(q => q.From<Tick>()
                    .Tumbling(r => r.TimestampUtc, new Windows { Minutes = new[] { 1, 5 } })
                    .GroupBy(r => new { r.Broker, r.Symbol })
                    .Select(g => new Bar
                    {
                        Broker = g.Key.Broker,
                        Symbol = g.Key.Symbol,
                        BucketStart = g.WindowStart(),
                        Open = g.EarliestByOffset(x => x.Bid),
                        High = g.Max(x => x.Bid),
                        Low = g.Min(x => x.Bid),
                        Close = g.LatestByOffset(x => x.Bid)
                    })
                );
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Import_Ticks_Define_Tumbling_Query_Then_Extract_Bars_Via_TimeBucket()
    {
        // Local RocksDB state may affect table materialization; clear it first
        try { PhysicalTestEnv.Cleanup.DeleteLocalRocksDbState(); } catch { }
        // Ensure env is ready (honor Docker runner env if set)
        var brokers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "127.0.0.1:39092";
        var srUrl = Environment.GetEnvironmentVariable("SCHEMA_REGISTRY_URL") ?? "http://127.0.0.1:18081";
        var ksqlUrl = Environment.GetEnvironmentVariable("KSQLDB_URL") ?? "http://127.0.0.1:18088";

        await PhysicalTestEnv.Health.WaitForKafkaAsync(brokers, TimeSpan.FromSeconds(180));
        await PhysicalTestEnv.Health.WaitForHttpOkAsync(srUrl.TrimEnd('/') + "/subjects", TimeSpan.FromSeconds(180));
        await PhysicalTestEnv.KsqlHelpers.WaitForKsqlReadyAsync(ksqlUrl, TimeSpan.FromSeconds(180), graceMs: 3000);

        // Pre-create source and DLQ topics
        using (var admin = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = brokers }).Build())
        {
            try { await admin.CreateTopicsAsync(new[] { new TopicSpecification { Name = "ticks", NumPartitions = 1, ReplicationFactor = 1 } }); } catch { }
            await PhysicalTestEnv.TopicHelpers.WaitForTopicReady(admin, "ticks", 1, 1, TimeSpan.FromSeconds(60));
            try { await admin.CreateTopicsAsync(new[] { new TopicSpecification { Name = "dead-letter-queue", NumPartitions = 1, ReplicationFactor = 1 } }); } catch { }
            await PhysicalTestEnv.TopicHelpers.WaitForTopicReady(admin, "dead-letter-queue", 1, 1, TimeSpan.FromSeconds(60));
        }
        // Create context (may issue schema registration and DDLs)
        using var ctx = new TestContext();
        await ctx.WaitForEntityReadyAsync<Bar>(TimeSpan.FromSeconds(180));

        var broker = "B"; var symbol = "S";
        var baseTime = DateTime.UtcNow.AddMinutes(-8);

        // Import ticks (2 points in the first minute, 1 point in a later minute)
        await ctx.Ticks.AddAsync(new Tick { Broker = broker, Symbol = symbol, TimestampUtc = baseTime.AddSeconds(10), Bid = 100m });
        await ctx.Ticks.AddAsync(new Tick { Broker = broker, Symbol = symbol, TimestampUtc = baseTime.AddSeconds(40), Bid = 104m });
        await ctx.Ticks.AddAsync(new Tick { Broker = broker, Symbol = symbol, TimestampUtc = baseTime.AddMinutes(2).AddSeconds(5), Bid = 102m });

        // Wait until 1m/5m tables produce rows (ksqlDB table path)
        var rowsTable1m = await QueryRowsAsync(
            $"SELECT Broker, Symbol, BucketStart, Open, High, Low, Close FROM bar_1m_live WHERE Broker='{broker}' AND Symbol='{symbol}' LIMIT 10;",
            new Uri("http://127.0.0.1:18088"),
            TimeSpan.FromSeconds(15));
        var rowsTable5m = await QueryRowsAsync(
            $"SELECT Broker, Symbol, BucketStart, Open, High, Low, Close FROM bar_5m_live WHERE Broker='{broker}' AND Symbol='{symbol}' LIMIT 10;",
            new Uri("http://127.0.0.1:18088"),
            TimeSpan.FromSeconds(15));
        Assert.True(rowsTable1m.Count >= 1, "1m table pull returned no rows");
        Assert.True(rowsTable5m.Count >= 1, "5m table pull returned no rows");

        // Basic shape checks
        var first = rowsTable1m[0];
        Assert.Equal(broker, (string)first[0]!);
        Assert.Equal(symbol, (string)first[1]!);
    }

    // Removed TimeBucket-based wait; use direct Pull queries above

    private static async Task<System.Collections.Generic.List<object?[]>> QueryRowsAsync(string sql, Uri baseUrl, TimeSpan timeout)
    {
        using var http = new HttpClient { BaseAddress = baseUrl };
        var payload = new { sql, properties = new System.Collections.Generic.Dictionary<string, object>() };
        using var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");
        using var cts = new CancellationTokenSource(timeout);
        using var resp = await http.PostAsync("/query", content, cts.Token);
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadAsStringAsync(cts.Token);
        var rows = new System.Collections.Generic.List<object?[]>();
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(body);
            if (doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                foreach (var el in doc.RootElement.EnumerateArray())
                {
                    if (el.ValueKind == System.Text.Json.JsonValueKind.Object && el.TryGetProperty("row", out var rowEl))
                    {
                        if (rowEl.TryGetProperty("columns", out var cols) && cols.ValueKind == System.Text.Json.JsonValueKind.Array)
                        {
                            var arr = new object?[cols.GetArrayLength()];
                            int idx = 0;
                            foreach (var c in cols.EnumerateArray())
                            {
                                arr[idx++] = c.ValueKind switch
                                {
                                    System.Text.Json.JsonValueKind.Number => c.TryGetInt64(out var l) ? l : c.GetDouble(),
                                    System.Text.Json.JsonValueKind.String => c.GetString(),
                                    System.Text.Json.JsonValueKind.True => true,
                                    System.Text.Json.JsonValueKind.False => false,
                                    System.Text.Json.JsonValueKind.Null => null,
                                    _ => c.ToString()
                                };
                            }
                            rows.Add(arr);
                        }
                    }
                }
            }
        }
        catch { }
        return rows;
    }
}
