using System;
using System.Linq;
using Kafka.Ksql.Linq.Query.Analysis;
using Kafka.Ksql.Linq.Query.Adapters;
using Kafka.Ksql.Linq.Query.Dsl;
using Kafka.Ksql.Linq.Query.Pipeline;
using Xunit;

namespace Kafka.Ksql.Linq.Tests.Query.Analysis;

public class TopologicalOrderTests
{
    public class Rate
    {
        public string Broker { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public DateTime BucketStart { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
    }

    public class MarketSchedule
    {
        public string Broker { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public DateTime Open { get; set; }
        public DateTime Close { get; set; }
        public DateTime MarketDate { get; set; }
    }

    private static System.Linq.Expressions.Expression Build() =>
        ((System.Linq.Expressions.Expression<Func<KsqlQueryable<Rate>, object>>)(q => q
            .TimeFrame<MarketSchedule>(
                (r, s) => r.Broker == s.Broker && r.Symbol == s.Symbol && s.Open <= r.Timestamp && r.Timestamp < s.Close,
                s => s.MarketDate)
            .Tumbling(r => r.Timestamp, new[] { 1 }, null, null, null, null, null)
            .GroupBy(r => new { r.Broker, r.Symbol, BucketStart = r.Timestamp })
            .Select(g => new { g.Key.Broker, g.Key.Symbol, g.Key.BucketStart, Open = g.Max(x => x.Open) }))).Body;

    [Fact]
    public void Apply_Respects_Topological_Order_NoMissingDeps()
    {
        var qao = TumblingAnalyzer.Analyze(Build(), typeof(Rate));
        var (entities, dag) = DerivationPlanner.Plan(qao);
        var specs = QueryAdapter.Build(entities, dag).ToList();
        var order = dag.TopologicalSort().ToList();
        foreach (var spec in specs)
        {
            var idx = order.IndexOf(spec.TargetId);
            foreach (var src in spec.Sources)
                Assert.True(order.IndexOf(src) < idx);
        }
    }
}
