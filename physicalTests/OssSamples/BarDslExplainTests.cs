using System;
using Kafka.Ksql.Linq;
using Kafka.Ksql.Linq.Configuration;
using Kafka.Ksql.Linq.Core.Attributes;
using Kafka.Ksql.Linq.Core.Abstractions;
using Kafka.Ksql.Linq.Query.Dsl;
using Kafka.Ksql.Linq.Query.Builders;
using Xunit;
using System.Threading.Tasks;
using Kafka.Ksql.Linq.Core.Modeling;

namespace Kafka.Ksql.Linq.Tests.Integration;

/// <summary>
/// OnModelCreating → ToQuery → Materialize(SQL) → Verify の流れに統一したDSLテスト。
/// </summary>
public class BarDslExplainTests
{
    [KsqlStream]
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
        public double Close { get; set; }
    }


    private sealed class TestContext : KsqlContext
    {
        public TestContext() : base(new KsqlDslOptions
        {
            Common = new CommonSection { BootstrapServers = "localhost:9092" },
            SchemaRegistry = new Kafka.Ksql.Linq.Core.Configuration.SchemaRegistrySection { Url = "http://localhost:8081" },
            KsqlDbUrl = "http://localhost:8088"
        }) { }
        // 物理テスト: スキーマ登録を有効化
        protected override bool SkipSchemaRegistration => false;

        // 入力イベントセット（AddAsyncで物理投入）
        public EventSet<Rate> Rates { get; set; } = null!;
        protected override void OnModelCreating(IModelBuilder modelBuilder)
        {
            // 入力は属性（[KsqlStream]/[KsqlTopic]/[KsqlTimestamp]）で扱う
            // 1m/5mの足は単一DSLで展開（minutes: new[]{1,5}）
            modelBuilder.Entity<Bar>()
                .ToQuery(q => q.From<Rate>()
                    .Tumbling(r => r.Timestamp, minutes: new[] { 1, 5 })
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
                        }));

        }
    }

    [Fact]
    public async Task Tumbling_1m_5m_Live_Ohlc_Materialize_And_Verify()
    {
        await using var ctx = new TestContext();
        // 環境初期化（ksqlDBの起動確認）
        await PhysicalTestEnv.KsqlHelpers.WaitForKsqlReadyAsync("http://localhost:8088", TimeSpan.FromSeconds(180), graceMs: 2000);
        // OSSのスキーマ登録・DDL発行を待機（Rateストリーム）
        await ctx.WaitForEntityReadyAsync<Rate>(TimeSpan.FromSeconds(60));
        var t0 = new DateTime(2020, 1, 6, 0, 0, 0, DateTimeKind.Utc);
        // 物理投入（Rate トピックへ）: 1m×2本, 5m×1本を検証できる十分なティックを投入
        // 1分バケット [00:00,00:01): O=100, H=110, L=90, C=105
        await ctx.Rates.AddAsync(new Rate { Broker = "B1", Symbol = "S1", Timestamp = t0.AddSeconds(1),  Bid = 100 });
        await ctx.Rates.AddAsync(new Rate { Broker = "B1", Symbol = "S1", Timestamp = t0.AddSeconds(15), Bid = 110 });
        await ctx.Rates.AddAsync(new Rate { Broker = "B1", Symbol = "S1", Timestamp = t0.AddSeconds(30), Bid = 90  });
        await ctx.Rates.AddAsync(new Rate { Broker = "B1", Symbol = "S1", Timestamp = t0.AddSeconds(59), Bid = 105 });
        // 1分バケット [00:01,00:02): O=200, H=210, L=195, C=195
        await ctx.Rates.AddAsync(new Rate { Broker = "B1", Symbol = "S1", Timestamp = t0.AddMinutes(1).AddSeconds(5),  Bid = 200 });
        await ctx.Rates.AddAsync(new Rate { Broker = "B1", Symbol = "S1", Timestamp = t0.AddMinutes(1).AddSeconds(20), Bid = 210 });
        await ctx.Rates.AddAsync(new Rate { Broker = "B1", Symbol = "S1", Timestamp = t0.AddMinutes(1).AddSeconds(50), Bid = 195 });
        // 5分バケット [00:00,00:05) の充実（追加ティック）
        await ctx.Rates.AddAsync(new Rate { Broker = "B1", Symbol = "S1", Timestamp = t0.AddMinutes(2).AddSeconds(10), Bid = 220 });
        await ctx.Rates.AddAsync(new Rate { Broker = "B1", Symbol = "S1", Timestamp = t0.AddMinutes(3).AddSeconds(10), Bid = 215 });
        await ctx.Rates.AddAsync(new Rate { Broker = "B1", Symbol = "S1", Timestamp = t0.AddMinutes(4).AddSeconds(45), Bid = 205 });
        // CSASはOSSが生成（UTでビルダー検証済）。ここでは出力の行出力のみ確認する。
        // 行確認（LIMITにより終了）
        async Task<int> CountEventuallyAsync(string table, int limit)
        {
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(90);
            Exception? last = null;
            while (DateTime.UtcNow < deadline)
            {
                try
                {
                    var c = await ctx.QueryStreamCountAsync($"SELECT * FROM {table} EMIT CHANGES LIMIT {limit};", TimeSpan.FromSeconds(30));
                    if (c > 0) return c;
                }
                catch (Exception ex) { last = ex; }
                await Task.Delay(1000);
            }
            throw new TimeoutException($"No rows for {table}. Last: {last?.Message}");
        }
        var c1 = await CountEventuallyAsync("bar_1m_live", 2);
        var c5 = await CountEventuallyAsync("bar_5m_live", 1);
        Assert.True(c1 >= 2, $"expected >=2 rows for 1m, got {c1}");
        Assert.True(c5 >= 1, $"expected >=1 row for 5m, got {c5}");

        // 全行を取得して OHLC の正しさを検証（バケット開始で特定）
        static long Ms(DateTime dt) => (long)(dt - DateTime.UnixEpoch).TotalMilliseconds;
        var bs00 = Ms(t0);
        var bs01 = Ms(t0.AddMinutes(1));

        var rows1m = await ctx.QueryRowsAsync("SELECT BucketStart, Open, High, Low, Close FROM bar_1m_live WHERE Broker='B1' AND Symbol='S1';", TimeSpan.FromSeconds(30));
        bool ok1 = false, ok2 = false;
        foreach (var r in rows1m)
        {
            var b = Convert.ToInt64(r[0]!);
            var o = Convert.ToDouble(r[1]!);
            var h = Convert.ToDouble(r[2]!);
            var l = Convert.ToDouble(r[3]!);
            var c = Convert.ToDouble(r[4]!);
            if (b == bs00 && o==100 && h==110 && l==90 && c==105) ok1 = true;
            if (b == bs01 && o==200 && h==210 && l==195 && c==195) ok2 = true;
        }
        Assert.True(ok1, "1m OHLC for 00:00 mismatch");
        Assert.True(ok2, "1m OHLC for 00:01 mismatch");

        var rows5m = await ctx.QueryRowsAsync("SELECT BucketStart, Open, High, Low, Close FROM bar_5m_live WHERE Broker='B1' AND Symbol='S1';", TimeSpan.FromSeconds(30));
        bool ok5 = false;
        foreach (var r in rows5m)
        {
            var b = Convert.ToInt64(r[0]!);
            var o = Convert.ToDouble(r[1]!);
            var h = Convert.ToDouble(r[2]!);
            var l = Convert.ToDouble(r[3]!);
            var c = Convert.ToDouble(r[4]!);
            if (b == bs00 && o==100 && h==220 && l==90 && c==205) ok5 = true;
        }
        Assert.True(ok5, "5m OHLC mismatch");

        // 後片付け
        await ctx.ExecuteStatementAsync("TERMINATE ALL;");
        await ctx.ExecuteStatementAsync("DROP TABLE IF EXISTS bar_1m_live DELETE TOPIC;");
        await ctx.ExecuteStatementAsync("DROP TABLE IF EXISTS bar_5m_live DELETE TOPIC;");
    }
}
