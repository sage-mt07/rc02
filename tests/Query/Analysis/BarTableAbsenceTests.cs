using Kafka.Ksql.Linq.Query.Analysis;
using Kafka.Ksql.Linq.Query.Dsl;
using System;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace Kafka.Ksql.Linq.Tests.Query.Analysis;

public class BarTableAbsenceTests
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
    public void Tumbling_DoesNot_Create_Base_Bar_Table()
    {
        Expression expr = ((Expression<Func<KsqlQueryable<Rate>, object>>)(q => q
            .TimeFrame<MarketSchedule>(
                (r, s) =>
                    r.Broker == s.Broker &&
                    r.Symbol == s.Symbol &&
                    s.Open <= r.Timestamp &&
                    r.Timestamp < s.Close,
                s => s.MarketDate)
            .Tumbling(r => r.Timestamp, new[] { 1, 5 }, null, null, null, null, null)
            .GroupBy(r => new { r.Broker, r.Symbol, BucketStart = r.Timestamp })
            .Select(g => new Bar
            {
                Broker = g.Key.Broker,
                Symbol = g.Key.Symbol,
                BucketStart = g.WindowStart(),
                Open = g.EarliestByOffset(x => x.Bid),
                High = g.Max(x => x.Bid),
                Low = g.Min(x => x.Bid),
                Close = g.LatestByOffset(x => x.Bid)
            }))).Body;

        var qao = TumblingAnalyzer.Analyze(expr, typeof(Rate));
        var (entities, _) = DerivationPlanner.Plan(qao);

        Assert.DoesNotContain(entities, e => e.Id.Equals("bar", StringComparison.OrdinalIgnoreCase));
        Assert.All(entities.Where(e => e.Role != Role.Hb), e => Assert.StartsWith("bar_", e.Id));
    }
}
