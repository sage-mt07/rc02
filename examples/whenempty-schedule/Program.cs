using System;
using System.Collections.Generic;
using System.Linq;

// WhenEmpty schedule sample based on docs/chart.md
// Chart steps: From → TimeFrame → Tumbling → GroupBy/Select → (optional) WhenEmpty → Rollup

public record Tick(string Broker, string Symbol, DateTime TimestampUtc, decimal Bid);

public record MarketSchedule(string Broker, string Symbol, DateTime OpenTimeUtc, DateTime CloseTimeUtc);

public record Rate(string Broker, string Symbol, DateTime BucketStart, decimal Open, decimal High, decimal Low, decimal Close);

static class WhenEmptyChart
{
    private static DateTime FloorToMinute(DateTime t)
        => new DateTime((t.Ticks / TimeSpan.TicksPerMinute) * TimeSpan.TicksPerMinute, DateTimeKind.Utc);

    private static IEnumerable<DateTime> EnumerateMinutes(DateTime startUtc, DateTime endUtc)
    {
        for (var t = FloorToMinute(startUtc); t < FloorToMinute(endUtc); t = t.AddMinutes(1))
            yield return t;
    }

    // Step: GroupBy/Select for 1-minute OHLC
    private static Rate? AggregateMinute(string broker, string symbol, DateTime bucket, IReadOnlyList<Tick> ticks)
    {
        var next = bucket.AddMinutes(1);
        var group = ticks.Where(t => t.TimestampUtc >= bucket && t.TimestampUtc < next).ToList();
        if (group.Count == 0) return null;
        var o = group.First().Bid;
        var h = group.Max(x => x.Bid);
        var l = group.Min(x => x.Bid);
        var c = group.Last().Bid;
        return new Rate(broker, symbol, bucket, o, h, l, c);
    }

    // Step: WhenEmpty (fill with previous close)
    private static Rate FillFromPrevious(string broker, string symbol, DateTime bucket, Rate prev)
    {
        var c = prev.Close;
        return new Rate(broker, symbol, bucket, c, c, c, c);
    }

    // Build 1m bars with optional WhenEmpty filler as in docs/chart.md
    public static List<Rate> Build1m(string broker, string symbol, MarketSchedule sch, IEnumerable<Tick> source)
    {
        if (sch.OpenTimeUtc.Kind != DateTimeKind.Utc || sch.CloseTimeUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Schedule must be UTC");

        // From → TimeFrame (schedule) → Tumbling(1m)
        var ticks = source
            .Where(t => t.Broker == broker && t.Symbol == symbol)
            .Where(t => t.TimestampUtc >= sch.OpenTimeUtc && t.TimestampUtc < sch.CloseTimeUtc)
            .OrderBy(t => t.TimestampUtc)
            .ToList();

        var bars = new List<Rate>();
        Rate? prev = null;
        foreach (var minute in EnumerateMinutes(sch.OpenTimeUtc, sch.CloseTimeUtc))
        {
            var agg = AggregateMinute(broker, symbol, minute, ticks);
            if (agg != null)
            {
                bars.Add(agg);
                prev = agg;
            }
            else if (prev != null)
            {
                // WhenEmpty: 欠損バケットを直前 Close で埋める（O=H=L=C=prev.Close）
                var filled = FillFromPrevious(broker, symbol, minute, prev);
                bars.Add(filled);
                prev = filled;
            }
            else
            {
                // 先頭バケットが欠損で前値がない場合はスキップ（後続に値が出れば以降は埋まる）
            }
        }
        return bars;
    }

    // Optional: 5m rollup from 1m bars
    public static List<Rate> Rollup5m(IEnumerable<Rate> bars)
    {
        static DateTime Floor5m(DateTime t) => new DateTime((t.Ticks / TimeSpan.FromMinutes(5).Ticks) * TimeSpan.FromMinutes(5).Ticks, DateTimeKind.Utc);
        return bars
            .GroupBy(b => (b.Broker, b.Symbol, Bucket: Floor5m(b.BucketStart)))
            .OrderBy(g => g.Key.Bucket)
            .Select(g => new Rate(
                g.First().Broker,
                g.First().Symbol,
                g.Key.Bucket,
                g.OrderBy(x => x.BucketStart).First().Open,
                g.Max(x => x.High),
                g.Min(x => x.Low),
                g.OrderBy(x => x.BucketStart).Last().Close))
            .ToList();
    }
}

class Program
{
    static void Main()
    {
        var broker = "B1";
        var symbol = "S1";
        var today = DateTime.UtcNow.Date;
        var schedule = new MarketSchedule(broker, symbol, today.AddHours(0), today.AddHours(0).AddMinutes(10));

        // Synthetic data with a missing minute (60-119s)
        var ticks = new List<Tick>();
        var ts = schedule.OpenTimeUtc;
        decimal price = 100m;
        for (int s = 0; s < 600; s++)
        {
            if (s < 60 || s >= 120)
                ticks.Add(new Tick(broker, symbol, ts, Math.Round(price, 4, MidpointRounding.AwayFromZero)));
            ts = ts.AddSeconds(1);
            price += 0.01m;
        }

        var bars1m = WhenEmptyChart.Build1m(broker, symbol, schedule, ticks);
        Console.WriteLine($"1m bars: {bars1m.Count}");
        foreach (var b in bars1m.Take(6))
            Console.WriteLine($"{b.BucketStart:HH:mm} O:{b.Open} H:{b.High} L:{b.Low} C:{b.Close}");

        var bars5m = WhenEmptyChart.Rollup5m(bars1m);
        Console.WriteLine($"5m bars: {bars5m.Count}");
        foreach (var b in bars5m)
            Console.WriteLine($"[5m] {b.BucketStart:HH:mm} O:{b.Open} H:{b.High} L:{b.Low} C:{b.Close}");
    }
}