using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DailyComparisonLib;
using DailyComparisonLib.Models;
using Kafka.Ksql.Linq.Core.Extensions;

static class PriceGen
{
    public static decimal Next(Random rnd, decimal center = 100m, decimal span = 1.0m)
    {
        var delta = (decimal)(rnd.NextDouble() - 0.5) * 2 * span;
        return Math.Round(center + delta, 4);
    }
}

class Program
{
    static async Task<int> Main(string[] args)
    {
        // Defaults
        var broker = Environment.GetEnvironmentVariable("BROKER") ?? "demo";
        var symbol = Environment.GetEnvironmentVariable("SYMBOL") ?? "EURUSD";
        var durationMin = 10; // 10 minutes produces two 5m bars
        var graceWaitSec = 30; // wait for finalization

        foreach (var a in args)
        {
            if (a.StartsWith("--broker=")) broker = a.Substring("--broker=".Length);
            if (a.StartsWith("--symbol=")) symbol = a.Substring("--symbol=".Length);
            if (a.StartsWith("--duration=") && int.TryParse(a.Substring("--duration=".Length), out var m)) durationMin = m;
            if (a.StartsWith("--grace-wait=") && int.TryParse(a.Substring("--grace-wait=".Length), out var s)) graceWaitSec = s;
        }

        await using var context = MyKsqlContext.FromAppSettings("appsettings.json");

        // Ensure schedule exists for today to enable bar generation.
        var today = DateTime.UtcNow.Date;
        var scheduleUpdater = new ScheduleUpdater(context);
        await scheduleUpdater.UpdateAsync(new []{
            new MarketSchedule {
                Broker = broker,
                Symbol = symbol,
                Date = today,
                OpenTime = today,
                CloseTime = today.AddDays(1) // 24h open
            }
        }, CancellationToken.None);

        // Produce 1 tick/second for durationMin minutes
        Console.WriteLine($"[produce] {broker}/{symbol} @1/s for {durationMin} min");
        var rnd = new Random();
        var until = DateTime.UtcNow.AddMinutes(durationMin);
        while (DateTime.UtcNow < until)
        {
            var ts = DateTime.UtcNow;
            var id = ts.Ticks;
            var price = PriceGen.Next(rnd);
            var rate = new Rate { Broker = broker, Symbol = symbol, RateId = id, RateTimestamp = ts, Bid = price, Ask = price + 0.01m };
            await context.Set<Rate>().AddAsync(rate);
            await Task.Delay(1000);
        }

        Console.WriteLine($"[wait] waiting {graceWaitSec}s for window finalization...");
        await Task.Delay(TimeSpan.FromSeconds(graceWaitSec));

        // Query 1m and 5m bars
        var oneMin = await context.Set<RateCandle>().Window(1).ToListAsync();
        var fiveMin = await context.Set<RateCandle>().Window(5).ToListAsync();

        // Filter to today + symbol/broker
        DateTime BarTime(RateCandle c) => c.BarTime;
        oneMin = oneMin.Where(c => c.Broker == broker && c.Symbol == symbol && BarTime(c).Date == today).ToList();
        fiveMin = fiveMin.Where(c => c.Broker == broker && c.Symbol == symbol && BarTime(c).Date == today).OrderBy(c => c.BarTime).ToList();

        Console.WriteLine($"[stats] 1m bars: {oneMin.Count}, 5m bars: {fiveMin.Count}");

        // Floor BarTime to 5-minute bucket start
        static DateTime FloorTo5Min(DateTime dt)
        {
            var ticks5m = TimeSpan.FromMinutes(5).Ticks;
            var floored = new DateTime((dt.Ticks / ticks5m) * ticks5m, DateTimeKind.Utc);
            return floored;
        }

        // Build 1m→5m synthetic rollup
        var grouped1m = oneMin
            .GroupBy(c => FloorTo5Min(BarTime(c)))
            .ToDictionary(g => g.Key, g => new {
                Open = g.OrderBy(x => BarTime(x)).First().Open,
                High = g.Max(x => x.High),
                Low = g.Min(x => x.Low),
                Close = g.OrderBy(x => BarTime(x)).Last().Close
            });

        // Verify each 5m bar equals the rollup from 1m bars
        var mismatches = new List<string>();
        foreach (var b5 in fiveMin)
        {
            if (!grouped1m.TryGetValue(b5.BarTime, out var roll))
            {
                mismatches.Add($"[missing] no 1m group for 5m {b5.BarTime:HH:mm}");
                continue;
            }

            bool eq(decimal a, decimal b) => a == b; // exact compare by design

            if (!eq(b5.Open, roll.Open) || !eq(b5.High, roll.High) || !eq(b5.Low, roll.Low) || !eq(b5.Close, roll.Close))
            {
                mismatches.Add($"[mismatch] 5m {b5.BarTime:HH:mm} O:{b5.Open}/{roll.Open} H:{b5.High}/{roll.High} L:{b5.Low}/{roll.Low} C:{b5.Close}/{roll.Close}");
            }
        }

        // Expect roughly durationMin/5 5m bars; for 10 minutes, 2 bars
        var expectedBars = Math.Max(1, durationMin / 5);
        Console.WriteLine($"[expect] ~{expectedBars} x 5m bars");

        if (mismatches.Count == 0 && fiveMin.Count >= expectedBars)
        {
            Console.WriteLine("[ok] 5m bars were produced and match 1m rollup (OHLC)");
            return 0;
        }

        Console.WriteLine("[result] mismatches:");
        foreach (var m in mismatches) Console.WriteLine(m);
        Console.WriteLine($"[fail] fiveMin.Count={fiveMin.Count}, expected≈{expectedBars}");
        return 1;
    }
}

