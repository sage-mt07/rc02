using Kafka.Ksql.Linq;
using Kafka.Ksql.Linq.Configuration;
using Kafka.Ksql.Linq.Core.Abstractions;
using Kafka.Ksql.Linq.Core.Attributes;
using Kafka.Ksql.Linq.Core.Modeling;
using Kafka.Ksql.Linq.Query.Dsl;
using Microsoft.Extensions.Logging;
using Xunit;
using Confluent.Kafka;
using Confluent.Kafka.Admin;

namespace Kafka.Ksql.Linq.Tests.Integration;

#nullable enable

/// <summary>
/// OnModelCreating → ToQuery → Materialize(SQL) → Verify の流れに統一したDSLテスト。
/// </summary>
public class BarDslExplainTests
{
    [KsqlTopic("deduprates")]
    public class Rate
    {
        [KsqlKey(1)] public string Broker { get; set; } = string.Empty;
        [KsqlKey(2)] public string Symbol { get; set; } = string.Empty;
        [KsqlTimestamp]
        public DateTime Timestamp { get; set; }
        public double Bid { get; set; }
    }

    public class Bar
    {
        [KsqlKey(1)] public string Broker { get; set; } = string.Empty;
        [KsqlKey(2)] public string Symbol { get; set; } = string.Empty;
        [KsqlKey(3)] public DateTime BucketStart { get; set; }
        public double Open { get; set; }
        public double High { get; set; }
        public double Low { get; set; }
        [KsqlTimeFrameClose] public double KsqlTimeFrameClose { get; set; }
    }


    private sealed class TestContext : KsqlContext
    {
        private static readonly ILoggerFactory _loggerFactory = LoggerFactory.Create(b =>
            b.AddConsole().SetMinimumLevel(LogLevel.Debug));

        public TestContext() : base(new KsqlDslOptions
        {
            Common = new CommonSection { BootstrapServers = "localhost:9092" },
            SchemaRegistry = new Kafka.Ksql.Linq.Core.Configuration.SchemaRegistrySection { Url = "http://localhost:8081" },
            KsqlDbUrl = "http://localhost:8088",
            Topics =
            {
                ["deduprates"] = new Kafka.Ksql.Linq.Configuration.Messaging.TopicSection
                {
                    Producer = new Kafka.Ksql.Linq.Configuration.Messaging.ProducerSection
                    {
                        Acks = "All",
                        EnableIdempotence = true,
                        LingerMs = 0,
                        DeliveryTimeoutMs = 30000,
                        Retries = 5,
                        MaxInFlightRequestsPerConnection = 1,
                        BatchSize = 16384,
                        RetryBackoffMs = 100,
                        CompressionType = "Snappy"
                    },
                    Creation = new Kafka.Ksql.Linq.Configuration.Messaging.TopicCreationSection
                    {
                        NumPartitions = 1,
                        ReplicationFactor = 1,
                        EnableAutoCreation = false
                    }
                }
            }
        }, _loggerFactory)
        { }
        // 物理テスト: スキーマ登録を有効化
        protected override bool SkipSchemaRegistration => false;

        // 入力イベントセット（AddAsyncで物理投入）
        public EventSet<Rate> Rates { get; set; } = null!;
        protected override void OnModelCreating(IModelBuilder modelBuilder)
        {
            // 入力は属性（[KsqlTopic]/[KsqlTimestamp]）で扱う
            // 1m/5mの足は単一DSLで展開（minutes: new[]{1,5}）
            modelBuilder.Entity<Bar>()
                .ToQuery(q => q.From<Rate>()
                    .Tumbling(r => r.Timestamp,new Windows { Minutes = new[] { 1, 5 } })
                    .GroupBy(r => new { r.Broker, r.Symbol })
                    .Select(g => new Bar
                    {
                        Broker = g.Key.Broker,
                        Symbol = g.Key.Symbol,
                        BucketStart = g.WindowStart(),
                        Open = g.EarliestByOffset(x => x.Bid),
                        High = g.Max(x => x.Bid),
                        Low = g.Min(x => x.Bid),
                        KsqlTimeFrameClose = g.LatestByOffset(x => x.Bid)
                    }));

        }
    }

    [Fact]
    public async Task Tumbling_1m_5m_Live_Ohlc_Materialize_And_Verify()
    {
        // 事前クリーンアップ（依存順にDROP）
        await PhysicalTestEnv.KsqlHelpers.TerminateAndDropBarArtifactsAsync("http://localhost:8088");
        await using var ctx = new TestContext();
        // 環境初期化（ksqlDBの起動確認）
        await PhysicalTestEnv.KsqlHelpers.WaitForKsqlReadyAsync("http://localhost:8088", TimeSpan.FromSeconds(180), graceMs: 2000);
        // 入力トピックを先に確実に用意（ksqlDBのDDLと競合しない）
        using (var admin = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = "localhost:9092" }).Build())
        {
            try { await admin.CreateTopicsAsync(new[] { new TopicSpecification { Name = "deduprates", NumPartitions = 1, ReplicationFactor = 1 } }); } catch { }
            await PhysicalTestEnv.TopicHelpers.WaitForTopicReady(admin, "deduprates", 1, 1, TimeSpan.FromSeconds(10));
        }
        // OSSのスキーマ登録・DDL発行を待機（Rateストリーム）
        await ctx.WaitForEntityReadyAsync<Rate>(TimeSpan.FromSeconds(60));
        // 現在時刻に合わせたバケットで評価（遅延・遡りによるドロップを回避）
        var now = DateTime.UtcNow;
        var t0 = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0, DateTimeKind.Utc);
        // CSASはOSSが生成（UTでビルダー検証済）。ここでは出力の行出力のみ確認する。
        // 行存在確認（テーブルのpullクエリでポーリング）
        async Task<int> CountEventuallyAsync(string table, int limit)
        {
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(180);
            Exception? last = null;
            while (DateTime.UtcNow < deadline)
            {
                try
                {
                    var pull = $"SELECT BucketStart, Open, High, Low, KsqlTimeFrameClose FROM {table} WHERE Broker='B1' AND Symbol='S1';";
                    var c = await ctx.QueryCountAsync(pull, TimeSpan.FromSeconds(15));
                    if (c >= limit) return c;
                }
                catch (Exception ex) { last = ex; }
                await Task.Delay(2000);
            }
            throw new TimeoutException($"No rows for {table}. Last: {last?.Message}");
        }
        // push起動は残しつつ、最終的な存在確認は pull で行う
        // 集計テーブルの生成を確認
        var wait1mTask = CountEventuallyAsync("bar_1m_live", 1);
        var wait5mTask = CountEventuallyAsync("bar_5m_live", 1);

        // 物理投入（Rate トピックへ）: 大量送信で到達性を担保（120件/約2分分）
        var bids = new[] { 100d, 110d, 90d, 105d, 200d, 210d, 195d, 220d, 215d, 205d };
        for (int i = 0; i < 120; i++)
        {
            var ts = t0.AddSeconds(i);
            var bid = bids[i % bids.Length];
            await ctx.Rates.AddAsync(new Rate { Broker = "B1", Symbol = "S1", Timestamp = ts, Bid = bid });
            // 軽い隙間を入れてバッファリング・フラッシュを促す
            await Task.Delay(5);
        }

        // 最低限の生成を待ったあと、pullで必要件数に達するまで待機
        _ = await wait1mTask; _ = await wait5mTask;

        async Task<int> WaitPullCountAsync(string table, int min, TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow + timeout;
            while (DateTime.UtcNow < deadline)
            {
                var sql = $"SELECT BucketStart, Open, High, Low, KsqlTimeFrameClose FROM {table} WHERE Broker='B1' AND Symbol='S1';";
                var cnt = await ctx.QueryCountAsync(sql, TimeSpan.FromSeconds(10));
                if (cnt >= min) return cnt;
                await Task.Delay(1000);
            }
            return 0;
        }
        var c1 = await WaitPullCountAsync("bar_1m_live", 2, TimeSpan.FromSeconds(120));
        var c5 = await WaitPullCountAsync("bar_5m_live", 1, TimeSpan.FromSeconds(120));
        Assert.True(c1 >= 2, $"expected >=2 rows for 1m, got {c1}");
        Assert.True(c5 >= 1, $"expected >=1 row for 5m, got {c5}");

        // 全行を取得して OHLC の正しさを検証（バケット開始で特定）
        static long Ms(DateTime dt) => (long)(dt - DateTime.UnixEpoch).TotalMilliseconds;
        var bs00 = Ms(t0);
        var bs01 = Ms(t0.AddMinutes(1));

        var rows1m = await ctx.QueryRowsAsync("SELECT BucketStart, Open, High, Low, KsqlTimeFrameClose FROM bar_1m_live WHERE Broker='B1' AND Symbol='S1';", TimeSpan.FromSeconds(30));
        bool ok1 = false, ok2 = false;
        foreach (var r in rows1m)
        {
            var b = Convert.ToInt64(r[0]!);
            var o = Convert.ToDouble(r[1]!);
            var h = Convert.ToDouble(r[2]!);
            var l = Convert.ToDouble(r[3]!);
            var c = Convert.ToDouble(r[4]!);
            if (b == bs00 && o == 100 && h == 110 && l == 90 && c == 105) ok1 = true;
            if (b == bs01 && o == 200 && h == 210 && l == 195 && c == 195) ok2 = true;
        }
        Assert.True(ok1, "1m OHLC for 00:00 mismatch");
        Assert.True(ok2, "1m OHLC for 00:01 mismatch");

        var rows5m = await ctx.QueryRowsAsync("SELECT BucketStart, Open, High, Low, KsqlTimeFrameClose FROM bar_5m_live WHERE Broker='B1' AND Symbol='S1';", TimeSpan.FromSeconds(30));
        bool ok5 = false;
        foreach (var r in rows5m)
        {
            var b = Convert.ToInt64(r[0]!);
            var o = Convert.ToDouble(r[1]!);
            var h = Convert.ToDouble(r[2]!);
            var l = Convert.ToDouble(r[3]!);
            var c = Convert.ToDouble(r[4]!);
            if (b == bs00 && o == 100 && h == 220 && l == 90 && c == 205) ok5 = true;
        }
        Assert.True(ok5, "5m OHLC mismatch");

        // 後片付け
        await ctx.ExecuteStatementAsync("TERMINATE ALL;");
        await ctx.ExecuteStatementAsync("DROP TABLE IF EXISTS bar_1m_live DELETE TOPIC;");
        await ctx.ExecuteStatementAsync("DROP TABLE IF EXISTS bar_5m_live DELETE TOPIC;");
    }


}
