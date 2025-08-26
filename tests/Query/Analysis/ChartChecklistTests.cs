using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Kafka.Ksql.Linq;
using Kafka.Ksql.Linq.Core.Modeling;
using Kafka.Ksql.Linq.Query.Adapters;
using Kafka.Ksql.Linq.Query.Analysis;
using Kafka.Ksql.Linq.Query.Dsl;
using Kafka.Ksql.Linq.Query.Pipeline;
using Xunit;

namespace Kafka.Ksql.Linq.Tests.Query.Analysis;

public class ChartChecklistTests
{
    private class Rate
    {
        public string Broker { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
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
    public void Expression_Collects_Windows_TimeFrame_And_GroupByKeys()
    {
        Expression expr = ((Expression<Func<KsqlQueryable<Rate>, object>>)(q => q
            .TimeFrame<MarketSchedule>(
                (r, s) =>
                    r.Broker == s.Broker &&
                    r.Symbol == s.Symbol &&
                    s.Open <= r.Timestamp &&
                    r.Timestamp < s.Close,
                s => s.MarketDate)
            .Tumbling(r => r.Timestamp,
                new[] { 1 },
                new[] { 1 },
                new[] { 1 },
                new[] { 1 },
                null,
                null)
            .GroupBy(r => new { r.Broker, r.Symbol, BucketStart = r.Timestamp })
            .Select(g => g))).Body;

        var visitor = new MethodCallCollectorVisitor();
        visitor.Visit(expr);
        var res = visitor.Result;

        Assert.Equal(new[] { "1m", "1h", "1d", "1mo" }, res.Windows.ToArray());
        Assert.Equal(new[] { "Broker", "Symbol" }, res.BasedOnJoinKeys.ToArray());
        Assert.Equal(res.TimeKey, res.BasedOnOpen);
        Assert.Equal("Close", res.BasedOnClose);
        Assert.Equal("MarketDate", res.BasedOnDayKey);
        Assert.Equal(new[] { "Broker", "Symbol", "BucketStart" }, res.GroupByKeys.ToArray());
    }

    [Fact]
    public void Expression_Collects_Month_And_Week_Windows()
    {
        var q = Expression.Parameter(typeof(KsqlQueryable<Rate>), "q");
        var r = Expression.Parameter(typeof(Rate), "r");
        var timeLambda = Expression.Lambda(Expression.Property(r, nameof(Rate.Timestamp)), r);
        var method = typeof(KsqlQueryable<Rate>).GetMethods()
            .First(m => m.Name == "Tumbling" && m.GetParameters().Length == 7);
        var call = Expression.Call(q, method,
            timeLambda,
            Expression.Constant(null, typeof(int[])),
            Expression.Constant(null, typeof(int[])),
            Expression.Constant(null, typeof(int[])),
            Expression.Constant(new[] { 1 }),
            Expression.Constant(DayOfWeek.Monday, typeof(DayOfWeek?)),
            Expression.Constant(null, typeof(TimeSpan?))
        );
        var visitor = new MethodCallCollectorVisitor();
        visitor.Visit(call);
        var res = visitor.Result;

        Assert.Contains("1mo", res.Windows);
        Assert.Contains("1wk", res.Windows);
        Assert.Equal(DayOfWeek.Monday, res.WeekAnchor);
    }

    private static T ExecuteInScope<T>(Func<T> func)
    {
        using (ModelCreatingScope.Enter())
            return func();
    }

    [Fact]
    public void DmlGenerator_Translates_Ohlc_Aggregates()
    {
        Expression<Func<IGrouping<int, Rate>, object>> expr = g => new
        {
            Open  = g.EarliestByOffset(x => x.Open),
            High  = g.Max(x => x.High),
            Low   = g.Min(x => x.Low),
            Close = g.LatestByOffset(x => x.Close)
        };

        var generator = new DMLQueryGenerator();
        var sql = ExecuteInScope(() => generator.GenerateAggregateQuery("rates", expr.Body));

        Assert.Contains("EARLIEST_BY_OFFSET(Open) AS Open", sql);
        Assert.Contains("MAX(High) AS High", sql);
        Assert.Contains("MIN(Low) AS Low", sql);
        Assert.Contains("LATEST_BY_OFFSET(Close) AS Close", sql);
    }

    [Fact]
    public void QueryAdapter_Emits_Final_And_Live_Modes()
    {
        var entities = new[]
        {
            new DerivedEntity { Id = "agg_final_1m", Role = Role.AggFinal, Timeframe = new Timeframe(1, "m") },
            new DerivedEntity { Id = "live_1m", Role = Role.Live, Timeframe = new Timeframe(1, "m") },
            new DerivedEntity { Id = "final_1m", Role = Role.Final, Timeframe = new Timeframe(1, "m") }
        };
        var dag = new DerivationDag();
        var specs = QueryAdapter.Build(entities, dag);
        Assert.Equal("Window(TUMBLING,1m)+Emit(FINAL+GRACE)", specs.First(s => s.TargetId == "agg_final_1m").Operation);
        Assert.Equal("Window(TUMBLING,1m)+Emit(CHANGES)", specs.First(s => s.TargetId == "live_1m").Operation);
        Assert.Equal("Compose(AggFinal⟂Prev1m)", specs.First(s => s.TargetId == "final_1m").Operation);
    }

    [Fact]
    public void QueryAdapter_Rolls_Up_1m_To_1h()
    {
        var qao = new TumblingQao
        {
            TimeKey = "Timestamp",
            Windows = new List<Timeframe> { new(1, "m"), new(1, "h") },
            Keys = new[] { "Broker", "Symbol", "BucketStart" },
            Projection = new[] { "Broker", "Symbol", "BucketStart" },
            PocoShape = new[]
            {
                new ColumnShape("Broker", typeof(string), false),
                new ColumnShape("Symbol", typeof(string), false),
                new ColumnShape("Timestamp", typeof(DateTime), false),
                new ColumnShape("BucketStart", typeof(DateTime), false)
            },
            BasedOn = new BasedOnSpec(new[] { "Broker" }, "Open", "Close", "MarketDate")
        };
        var (entities, dag) = DerivationPlanner.Plan(qao);
        var specs = QueryAdapter.Build(entities, dag);
        var liveHour = specs.First(s => s.TargetId == "live_1h");
        Assert.Contains("live_1m", liveHour.Sources);
        Assert.Equal("Window(TUMBLING,1h)+Emit(CHANGES)", liveHour.Operation);
    }
}

