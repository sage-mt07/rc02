using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

// docs/chart.md の WhenEmpty を MarketSchedule と組み合わせて再現する最小サンプル
// - 1分バケットでOHLC集計
// - 空バケットは previous.Close で埋める（WhenEmpty）

public class Tick
{
    public string Broker { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; }
    public decimal Bid { get; set; }
}

public class MarketSchedule
{
    public string Broker { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public DateTime OpenTimeUtc { get; set; }
    public DateTime CloseTimeUtc { get; set; }
}

// docs/chart.md の足POCO（Rate）に合わせる
public class Rate
{
    public string Broker { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public DateTime BucketStart { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
}

static class WhenEmptyBars
{
    public static List<Rate> Build1mBarsWithWhenEmpty(string broker, string symbol, MarketSchedule sch, IEnumerable<Tick> ticks)
    {
        var tz = DateTimeKind.Utc;
        var open = sch.OpenTimeUtc;
        var close = sch.CloseTimeUtc;
        if (open.Kind != DateTimeKind.Utc || close.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Schedule must be UTC");

        // 対象銘柄のティックのみ、セッション範囲に絞る
        var src = ticks.Where(t => t.Broker == broker && t.Symbol == symbol)
                       .Where(t => t.TimestampUtc >= open && t.TimestampUtc < close)
                       .OrderBy(t => t.TimestampUtc)
                       .ToList();

        // 分境界リストを生成
        static DateTime FloorMin(DateTime dt) => new DateTime((dt.Ticks / TimeSpan.TicksPerMinute) * TimeSpan.TicksPerMinute, DateTimeKind.Utc);
        var start = FloorMin(open);
        var end = FloorMin(close);
        var minutes = new List<DateTime>();
        for (var t = start; t < end; t = t.AddMinutes(1)) minutes.Add(t);

        var bars = new List<Rate>();
        Rate? prev = null;
        int idx = 0;
        foreach (var m in minutes)
        {
            var next = m.AddMinutes(1);
            var group = new List<Tick>();
            while (idx < src.Count && src[idx].TimestampUtc >= m && src[idx].TimestampUtc < next)
            {
                group.Add(src[idx]);
                idx++;
            }

            if (group.Count > 0)
            {
                var o = group.First().Bid;
                var h = group.Max(x => x.Bid);
                var l = group.Min(x => x.Bid);
                var c = group.Last().Bid;
                var bar = new Rate { Broker = broker, Symbol = symbol, BucketStart = m, Open = o, High = h, Low = l, Close = c };
                bars.Add(bar);
                prev = bar;
            }
            else if (prev != null)
            {
                // WhenEmpty: 直前の Close で埋める（O=H=L=C=prev.Close）
                var c = prev.Close;
                var bar = new Rate { Broker = broker, Symbol = symbol, BucketStart = m, Open = c, High = c, Low = c, Close = c };
                bars.Add(bar);
                prev = bar;
            }
            else
            {
                // 初回に前日引継ぎが無いケースはスキップ（本番は prev_1m を参照）
            }
        }
        return bars;
    }
}

class Program
{
    static void Main()
    {
        var broker = "B1"; var symbol = "S1";
        var today = DateTime.UtcNow.Date;
        var sch = new MarketSchedule
        {
            Broker = broker,
            Symbol = symbol,
            OpenTimeUtc = today.AddHours(0),
            CloseTimeUtc = today.AddHours(0).AddMinutes(10) // 10分だけの短いセッション
        };

        // 1秒毎に+0.01の決定列。2分目を丸ごと欠損させ、WhenEmpty の補完を観察する
        var ticks = new List<Tick>();
        var cursor = sch.OpenTimeUtc;
        decimal price = 100m;
        for (int s = 0; s < 600; s++)
        {
            if (!(s >= 60 && s < 120)) // 2分目は欠損
            {
                ticks.Add(new Tick { Broker = broker, Symbol = symbol, TimestampUtc = cursor, Bid = Math.Round(price, 4, MidpointRounding.AwayFromZero) });
            }
            cursor = cursor.AddSeconds(1);
            price += 0.01m;
        }

        var bars = WhenEmptyBars.Build1mBarsWithWhenEmpty(broker, symbol, sch, ticks);

        Console.WriteLine("ticks (first 80s; missing 60-119s):");
        foreach (var t in ticks.Where(t => t.TimestampUtc < sch.OpenTimeUtc.AddSeconds(80)))
            Console.WriteLine($"{t.TimestampUtc:HH:mm:ss} {t.Bid:F4}");

        Console.WriteLine($"bars(1m)={bars.Count}");
        foreach (var b in bars.Take(6))
        {
            Console.WriteLine($"{b.BucketStart:HH:mm} O:{b.Open} H:{b.High} L:{b.Low} C:{b.Close}");
        }
        // 5分ロールアップ（1分のロールアップ）。WhenEmpty で埋まった1分も反映され、常に5分バーが得られる
        static DateTime Floor5m(DateTime t) => new DateTime((t.Ticks / TimeSpan.FromMinutes(5).Ticks) * TimeSpan.FromMinutes(5).Ticks, DateTimeKind.Utc);
        var bars5 = bars
            .GroupBy(b => Floor5m(b.BucketStart))
            .OrderBy(g => g.Key)
            .Select(g => new Rate
            {
                Broker = broker,
                Symbol = symbol,
                BucketStart = g.Key,
                Open = g.OrderBy(x => x.BucketStart).First().Open,
                High = g.Max(x => x.High),
                Low = g.Min(x => x.Low),
                Close = g.OrderBy(x => x.BucketStart).Last().Close
            })
            .ToList();

        Console.WriteLine($"bars(5m)={bars5.Count}");
        foreach (var b in bars5)
            Console.WriteLine($"[5m] {b.BucketStart:HH:mm} O:{b.Open} H:{b.High} L:{b.Low} C:{b.Close}");

        // 2分目(=open+1m)が WhenEmpty により O=H=L=C=前分の Close で埋まること、
        // かつ 5分バーがロールアップで得られることを示す
    }
}
