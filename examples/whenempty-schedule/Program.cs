using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Kafka.Ksql.Linq;
using Kafka.Ksql.Linq.Core.Attributes;
using Kafka.Ksql.Linq.Core.Modeling;
using Kafka.Ksql.Linq.Query.Dsl;
using Kafka.Ksql.Linq.Runtime;

// WhenEmpty schedule sample (DSL-first, aligned with physical tests)
// Steps: From → TimeFrame → Tumbling → GroupBy/Select → WhenEmpty → Rollup
// Note: Select must include exactly one WindowStart() (bucket column)

public class Tick
{
    [KsqlKey(1)] public string Broker { get; set; } = string.Empty;
    [KsqlKey(2)] public string Symbol { get; set; } = string.Empty;
    [KsqlTimestamp] public DateTime TimestampUtc { get; set; }
    public decimal Bid { get; set; }
}

public class MarketSchedule
{
    [KsqlKey(1)] public string Broker { get; set; } = string.Empty;
    [KsqlKey(2)] public string Symbol { get; set; } = string.Empty;
    public DateTime OpenTimeUtc { get; set; }
    public DateTime CloseTimeUtc { get; set; }
    public DateTime MarketDate { get; set; }
}

public class Bar
{
    [KsqlKey(1)] public string Broker { get; set; } = string.Empty;
    [KsqlKey(2)] public string Symbol { get; set; } = string.Empty;
    [KsqlKey(3)] public DateTime BucketStart { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
}

public sealed class SampleContext : KsqlContext
{
    public SampleContext() : base(new Kafka.Ksql.Linq.Configuration.KsqlDslOptions()) { }
    public EventSet<Tick> Ticks { get; set; } = null!;

    protected override void OnModelCreating(IModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Bar>()
            .ToQuery(q => q.From<Tick>()
                .TimeFrame<MarketSchedule>((r, s) =>
                       r.Broker == s.Broker
                    && r.Symbol == s.Symbol
                    && s.OpenTimeUtc <= r.TimestampUtc && r.TimestampUtc < s.CloseTimeUtc,
                    dayKey: s => s.MarketDate)
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
                // App defines filler policy; pipeline materializes Hb/Prev/Fill
                .WhenEmpty((prev, next) => next)
            );
    }
}

class Program
{
    static async Task Main()
    {
        // 1) DSL 定義（物理パイプラインで Hb/Prev/Fill を構築する前提の宣言）
        using var ctx = new SampleContext();

        // 2) 物理環境へデータ投入（欠損1分を含む Ticks）
        var broker = "B1"; var symbol = "S1";
        var t0 = DateTime.UtcNow.AddMinutes(-10);
        await ctx.Ticks.AddAsync(new Tick { Broker = broker, Symbol = symbol, TimestampUtc = t0.AddSeconds(1), Bid = 100m });
        await ctx.Ticks.AddAsync(new Tick { Broker = broker, Symbol = symbol, TimestampUtc = t0.AddSeconds(20), Bid = 105m });
        await ctx.Ticks.AddAsync(new Tick { Broker = broker, Symbol = symbol, TimestampUtc = t0.AddSeconds(40), Bid = 99m });
        await ctx.Ticks.AddAsync(new Tick { Broker = broker, Symbol = symbol, TimestampUtc = t0.AddMinutes(2).AddSeconds(5), Bid = 101m });

        // 3) TimeBucket で 1m/5m を確認（KSQL Pull ベースのコンテキストを使用）
        var tbCtx = new KsqlDbBucketContext("http://localhost:8088");
        var rows1m = await TimeBucket.Get<Bar>(tbCtx, Period.Minutes(1)).ToListAsync(new[] { broker, symbol }, CancellationToken.None);
        var rows5m = await TimeBucket.Get<Bar>(tbCtx, Period.Minutes(5)).ToListAsync(new[] { broker, symbol }, CancellationToken.None);

        Console.WriteLine($"1m rows: {rows1m.Count}");
        foreach (var b in rows1m.OrderBy(x => x.BucketStart))
            Console.WriteLine($"{b.BucketStart:HH:mm} O:{b.Open} H:{b.High} L:{b.Low} C:{b.Close}");

        Console.WriteLine($"5m rows: {rows5m.Count}");
        foreach (var b in rows5m.OrderBy(x => x.BucketStart))
            Console.WriteLine($"[5m] {b.BucketStart:HH:mm} O:{b.Open} H:{b.High} L:{b.Low} C:{b.Close}");
    }

    // ITimeBucketContext 実装（ksqlDB Pull を内部で使用）
    sealed class KsqlDbBucketContext : ITimeBucketContext
    {
        private readonly string _baseUrl;
        public KsqlDbBucketContext(string baseUrl) { _baseUrl = baseUrl.TrimEnd('/'); }
        public ITimeBucketSet<T> Set<T>(string topic, Period period) where T : class
            => (ITimeBucketSet<T>)new KsqlDbBucketSet(_baseUrl, topic);
    }

    sealed class KsqlDbBucketSet : ITimeBucketSet<Bar>
    {
        private readonly string _baseUrl; private readonly string _topic;
        public KsqlDbBucketSet(string baseUrl, string topic) { _baseUrl = baseUrl; _topic = topic; }
        public async Task<List<Bar>> ToListAsync(IReadOnlyList<string> pkFilter, CancellationToken ct)
        {
            if (pkFilter.Count < 2) throw new ArgumentException("Filter must contain at least Broker and Symbol");
            var broker = pkFilter[0].Replace("'", "''");
            var symbol = pkFilter[1].Replace("'", "''");
            var sql = $"SELECT Broker, Symbol, BucketStart, Open, High, Low, Close FROM {_topic} WHERE Broker='{broker}' AND Symbol='{symbol}';";
            using var http = new HttpClient { BaseAddress = new Uri(_baseUrl) };
            var payload = new { sql, properties = new Dictionary<string, object>() };
            using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            using var resp = await http.PostAsync("/query", content, ct);
            resp.EnsureSuccessStatusCode();
            var body = await resp.Content.ReadAsStringAsync(ct);
            var list = new List<Bar>();
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in doc.RootElement.EnumerateArray())
                {
                    if (el.TryGetProperty("row", out var row) && row.ValueKind == JsonValueKind.Array)
                    {
                        var arr = row.EnumerateArray().ToArray();
                        var b = new Bar
                        {
                            Broker = arr[0].GetString() ?? string.Empty,
                            Symbol = arr[1].GetString() ?? string.Empty,
                            BucketStart = DateTime.UnixEpoch.AddMilliseconds(arr[2].GetInt64()).ToUniversalTime(),
                            Open = (decimal)arr[3].GetDouble(),
                            High = (decimal)arr[4].GetDouble(),
                            Low = (decimal)arr[5].GetDouble(),
                            Close = (decimal)arr[6].GetDouble()
                        };
                        list.Add(b);
                    }
                }
            }
            if (list.Count == 0)
                throw new InvalidOperationException("No rows matched the filter.");
            return list;
        }
    }
}
