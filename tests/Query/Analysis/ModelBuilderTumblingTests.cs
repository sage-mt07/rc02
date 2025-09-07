using System;
using System.Collections.Generic;
using System.Linq;
using Kafka.Ksql.Linq.Core.Modeling;
using Kafka.Ksql.Linq.Query.Analysis;
using Kafka.Ksql.Linq.Query.Dsl;
using Kafka.Ksql.Linq.Query.Adapters;
using Xunit;

namespace Kafka.Ksql.Linq.Tests.Query.Analysis;

public class ModelBuilderTumblingTests
{
    private class Rate
    {
        public string Broker { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public DateTime BucketStart { get; set; }
        public decimal Bid { get; set; }
    }

    private class Bar
    {
        public string Broker { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public DateTime BucketStart { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
    }

    private class MarketSchedule
    {
        public string Broker { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public DateTime Open { get; set; }
        public DateTime Close { get; set; }
        public DateTime MarketDate { get; set; }
    }

    [Fact]
    public void ToQuery_Tumbling_Builds_Derived_Models()
    {
        var builder = new ModelBuilder();
        builder.Entity<Bar>().ToQuery(q => q
            .From<Rate>()
            .TimeFrame<MarketSchedule>(
                (r, s) => r.Broker == s.Broker &&
                          r.Symbol == s.Symbol &&
                          s.Open <= r.Timestamp &&
                          r.Timestamp < s.Close,
                s => s.MarketDate)
            .Tumbling(r => r.Timestamp, new[] { 1, 5 }, null, null, null, null, null)
            .GroupBy(r => new { r.Broker, r.Symbol, r.BucketStart })
            .Select(g => new Bar
            {
                Broker = g.Key.Broker,
                Symbol = g.Key.Symbol,
                BucketStart = g.Key.BucketStart,
                Open = g.EarliestByOffset(x => x.Bid),
                High = g.Max(x => x.Bid),
                Low = g.Min(x => x.Bid),
                Close = g.LatestByOffset(x => x.Bid)
            }));

        _ = builder.GetAllEntityModels();

        var qao = new TumblingQao
        {
            TimeKey = "Timestamp",
            Windows = new List<Timeframe> { new(1, "m"), new(5, "m") },
            Keys = new[] { "Broker", "Symbol", "BucketStart" },
            Projection = new[] { "Broker", "Symbol", "BucketStart" },
            PocoShape = new[]
            {
                new ColumnShape("Broker", typeof(string), false),
                new ColumnShape("Symbol", typeof(string), false),
                new ColumnShape("Timestamp", typeof(DateTime), false),
                new ColumnShape("BucketStart", typeof(DateTime), false),
                new ColumnShape("Bid", typeof(decimal), false)
            },
            BasedOn = new BasedOnSpec(new[] { "Broker", "Symbol" }, "Open", "Close", "MarketDate")
        };

        var (entities, _) = DerivationPlanner.Plan(qao);
        var models = EntityModelAdapter.Adapt(entities);

        Assert.DoesNotContain(models, m =>
            string.Equals(m.TopicName, "bar", StringComparison.OrdinalIgnoreCase) ||
            m.AdditionalSettings.Values.Any(v => string.Equals(v as string, "bar", StringComparison.OrdinalIgnoreCase)));

        Assert.Contains(models, m => (string)m.AdditionalSettings["id"] == "bar_1m_live");
        Assert.Contains(models, m => (string)m.AdditionalSettings["id"] == "bar_5m_live");
    }
}
