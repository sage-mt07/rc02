using Kafka.Ksql.Linq;
using Kafka.Ksql.Linq.Application;
using Kafka.Ksql.Linq.Core.Abstractions;
using Kafka.Ksql.Linq.Core.Attributes;
using Examples.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Kafka.Ksql.Linq.Core.Modeling;

public class OneMinuteCandle
{
    [KsqlKey(order: 0)] public string Broker { get; set; } = string.Empty;
    [KsqlKey(order: 1)] public string Symbol { get; set; } = string.Empty;
    [KsqlTimestamp] public DateTime BarStart { get; set; }
    [KsqlDecimal(precision: 18, scale: 4)] public decimal Open { get; set; }
    [KsqlDecimal(precision: 18, scale: 4)] public decimal High { get; set; }
    [KsqlDecimal(precision: 18, scale: 4)] public decimal Low { get; set; }
    [KsqlDecimal(precision: 18, scale: 4)] public decimal Close { get; set; }
}

[KsqlTable]
public class MarketSchedule
{
    [KsqlKey(order: 0)] public string Broker { get; set; } = string.Empty;
    [KsqlKey(order: 1)] public string Symbol { get; set; } = string.Empty;
    [KsqlTimestamp] public DateTime OpenTime { get; set; }
    [KsqlTimestamp] public DateTime CloseTime { get; set; }
}

public class TumbleContext : KsqlContext
{
    public TumbleContext(KsqlContextOptions options) : base(options.Configuration!, options.LoggerFactory) { }
    public TumbleContext(IConfiguration configuration, ILoggerFactory? loggerFactory = null) : base(configuration, loggerFactory) { }

    public EventSet<DedupRateRecord> Rates { get; set; }
    public EventSet<OneMinuteCandle> Candles { get; set; }

    protected override void OnModelCreating(IModelBuilder b)
    {
        // Simple 1m tumbling OHLC
        b.Entity<OneMinuteCandle>().ToQuery(q => q
            .From<DedupRateRecord>()
            .Tumbling(x => x.Ts, new Kafka.Ksql.Linq.Query.Dsl.Windows { Minutes = new[] { 1 } })
            .GroupBy(x => new { x.Broker, x.Symbol })
            .Select(g => new OneMinuteCandle
            {
                Broker = g.Key.Broker,
                Symbol = g.Key.Symbol,
                BarStart = g.WindowStart(),
                Open = g.EarliestByOffset(x => x.Bid),
                High = g.Max(x => x.Bid),
                Low = g.Min(x => x.Bid),
                Close = g.LatestByOffset(x => x.Bid)
            }));

        // MarketSchedule 連携版（必要に応じて有効化）
        // b.Entity<OneMinuteCandle>().ToQuery(q => q
        //    .From<DedupRateRecord>()
        //    .Tumbling(x => x.Ts, new Kafka.Ksql.Linq.Query.Dsl.Windows { Minutes = new[] { 1 } })
        //    .TimeFrame<MarketSchedule>((r, s) => r.Broker == s.Broker && r.Symbol == s.Symbol && r.Ts >= s.OpenTime && r.Ts < s.CloseTime)
        //    .GroupBy(x => new { x.Broker, x.Symbol })
        //    .Select(g => new OneMinuteCandle
        //    {
        //        Broker = g.Key.Broker,
        //        Symbol = g.Key.Symbol,
        //        BarStart = g.WindowStart(),
        //        Open = g.EarliestByOffset(x => x.Bid),
        //        High = g.Max(x => x.Bid),
        //        Low = g.Min(x => x.Bid),
        //        Close = g.LatestByOffset(x => x.Bid)
        //    }));
    }
}

class Program
{
    static async Task Main(string[] args)
    {
        var cfg = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
        await using var ctx = new TumbleContext(cfg, LoggerFactory.Create(b => b.AddConsole()));

        Console.WriteLine("[tumbling] consuming 1m candles (Ctrl+C to stop)");
        using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromMinutes(1));
        await ctx.Candles.ForEachAsync(c =>
        {
            Console.WriteLine($"{c.Broker}/{c.Symbol} {c.BarStart:HH:mm} O:{c.Open} H:{c.High} L:{c.Low} C:{c.Close}");
            return Task.CompletedTask;
        }, cancellationToken: cts.Token);
    }
}
