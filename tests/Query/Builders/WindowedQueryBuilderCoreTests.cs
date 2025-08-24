using System;
using Kafka.Ksql.Linq.Query.Builders;
using Kafka.Ksql.Linq.Query.Pipeline;
using Xunit;

namespace Kafka.Ksql.Linq.Tests.Query.Builders;

public class WindowedQueryBuilderCoreTests
{
    private static QueryMetadata BaseMd() =>
        new QueryMetadata(DateTime.UtcNow, "cat")
            .WithProperty("basedOn/joinKeys", new[] { "Broker" })
            .WithProperty("basedOn/openProp", "Open")
            .WithProperty("basedOn/closeProp", "Close")
            .WithProperty("basedOn/dayKey", "MarketDate")
            .WithProperty("timeKey", "Ts");

    [Fact]
    public void Core_Builds_Live_Table_EmitChanges_SyncsOnlyOn1m()
    {
        var md1 = BaseMd()
            .WithProperty("input/1mLive", "src1")
            .WithProperty("sync/1mLive", "HB_1m");
        var q1 = LiveBuilder.Build(md1, "1m");
        Assert.StartsWith("TABLE src1", q1);
        Assert.Contains("EMIT CHANGES", q1);
        Assert.Contains("SYNC HB_1m", q1);

        var md5 = BaseMd().WithProperty("input/5mLive", "src5");
        var q5 = LiveBuilder.Build(md5, "5m");
        Assert.DoesNotContain("SYNC", q5);
    }

    [Fact]
    public void Core_Builds_AggFinal_FinalPlusGrace_WithBucketStartProjector()
    {
        var q = AggFinalBuilder.Build(BaseMd(), "1m");
        Assert.Contains("FINAL GRACE", q);
        Assert.Contains("SELECT WINDOWSTART AS BucketStart", q);
    }

    [Fact]
    public void Core_Builds_Final_Compose_AggOrPrev_SyncsOnlyOn1m()
    {
        var md1 = BaseMd()
            .WithProperty("input/1mFinal", "agg1")
            .WithProperty("sync/1mFinal", "HB_1m");
        var q1 = FinalBuilder.Build(md1, "1m");
        Assert.Contains("COMPOSE(agg1)", q1);
        Assert.Contains("SYNC HB_1m", q1);

        var md5 = BaseMd().WithProperty("input/5mFinal", "agg5");
        var q5 = FinalBuilder.Build(md5, "5m");
        Assert.DoesNotContain("SYNC", q5);
    }

    [Fact]
    public void Core_Applies_TimeFrame_Join_And_Boundary_To_All()
    {
        var md = BaseMd().WithProperty("input/1mLive", "s");
        var live = LiveBuilder.Build(md, "1m");
        var agg = AggFinalBuilder.Build(md, "1m");
        var fin = FinalBuilder.Build(md.WithProperty("input/1mFinal", "a"), "1m");
        foreach (var q in new[] { live, agg, fin })
            Assert.Contains("JOIN ON", q);
    }

    [Fact]
    public void Builders_Expand_TimeFrame_Join_And_Boundary_For_All_Roles()
    {
        var md = BaseMd().WithProperty("input/1mLive", "s");
        var live = LiveBuilder.Build(md, "1m");
        var agg = AggFinalBuilder.Build(md, "1m");
        var fin = FinalBuilder.Build(md.WithProperty("input/1mFinal", "a"), "1m");
        Assert.Contains("s.Open <= r.Ts", live);
        Assert.Contains("s.Open <= r.Ts", agg);
        Assert.Contains("s.Open <= r.Ts", fin);
    }
}
