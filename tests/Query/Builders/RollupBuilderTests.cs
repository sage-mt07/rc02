using System;
using System.Linq.Expressions;
using Kafka.Ksql.Linq.Query.Builders;
using Kafka.Ksql.Linq.Query.Builders.Core;
using Kafka.Ksql.Linq.Query.Dsl;
using Kafka.Ksql.Linq.Query.Pipeline;
using Kafka.Ksql.Linq.Query.Analysis;
using Xunit;

namespace Kafka.Ksql.Linq.Tests.Query.Builders;

public class RollupBuilderTests
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

    private static QueryMetadata BuildMetadata()
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
            .Select(g => g))).Body;
        var analysis = Analyze(expr);
        return analysis.ToMetadata();
    }

    private static ExpressionAnalysisResult Analyze(Expression expr)
    {
        var visitor = new MethodCallCollectorVisitor();
        visitor.Visit(expr);
        var result = visitor.Result;
        if (result.Windows.Count == 0)
            throw new InvalidOperationException("Tumbling windows are required");
        if (result.TimeKey == null)
            throw new InvalidOperationException("Time key is required");
        if (!result.GroupByKeys.Contains(result.TimeKey) && !result.GroupByKeys.Contains("BucketStart"))
            throw new InvalidOperationException("Time key must be part of GroupBy keys");
        return result;
    }

    [Fact]
    public void AggFinal_Builds_FinalWithGrace_Projects_BucketStart()
    {
        var md = BuildMetadata();
        var sql = AggFinalBuilder.Build(md, "1m");
        Assert.Contains("WINDOW TUMBLING(1m)", sql);
        Assert.Contains("EMIT FINAL GRACE", sql);
        Assert.Contains("BucketStart", sql);
    }

    [Fact]
    public void Live_1m_IsTable_EmitChanges_SyncsOn_HB1m()
    {
        var md = BuildMetadata();
        var sql = LiveBuilder.Build(md, "1m");
        Assert.Contains("TABLE 10sAgg WINDOW TUMBLING(1m)", sql);
        Assert.Contains("EMIT CHANGES", sql);
        Assert.Contains("SYNC HB_1m", sql);
    }

    [Fact]
    public void Live_5m_RollsUp_From_1mLive()
    {
        var md = BuildMetadata();
        var sql = LiveBuilder.Build(md, "5m");
        Assert.Contains("TABLE bar_1m_live WINDOW TUMBLING(5m)", sql);
        Assert.DoesNotContain("HB_1m", sql);
    }

    [Fact]
    public void Final_Composes_AggFinal_Or_Prev1m_NonNull()
    {
        var md = BuildMetadata();
        var sql = FinalBuilder.Build(md, "5m");
        Assert.Contains("COMPOSE(bar_5m_agg_final ⟂ bar_prev_1m)", sql);
        Assert.DoesNotContain("HB_1m", sql);
        var sql1 = FinalBuilder.Build(md, "1m");
        Assert.Contains("SYNC HB_1m", sql1);
    }

    [Fact]
    public void Builders_Expand_TimeFrame_Join_And_Boundary_For_All_Roles()
    {
        var md = BuildMetadata();
        var a = AggFinalBuilder.Build(md, "1m");
        var b = LiveBuilder.Build(md, "1m");
        var c = FinalBuilder.Build(md, "1m");
        foreach (var sql in new[] { a, b, c })
        {
            Assert.Contains("JOIN", sql);
            Assert.Contains("<=", sql);
            Assert.Contains("<", sql);
        }
    }

    [Fact]
    public void RoleSpec_Table()
    {
        var specLive = RoleTraits.For(Role.Live, new Timeframe(1, "m"));
        Assert.True(specLive.Window);
        Assert.Equal("CHANGES", specLive.Emit);
        Assert.True(specLive.SyncHb1m);

        var specAgg = RoleTraits.For(Role.AggFinal, new Timeframe(1, "m"));
        Assert.True(specAgg.Projector);

        var specFinal = RoleTraits.For(Role.Final, new Timeframe(5, "m"));
        Assert.False(specFinal.SyncHb1m);
    }
}
