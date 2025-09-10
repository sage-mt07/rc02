using Kafka.Ksql.Linq.Query.Builders;
using Kafka.Ksql.Linq.Query.Pipeline;
using System;
using Xunit;

namespace Kafka.Ksql.Linq.Tests.Query.Builders;

public class WindowedQueryBuilderCoreTests
{
    private static QueryMetadata BaseMd() =>
        new QueryMetadata(DateTime.UtcNow, "cat")
            .WithProperty("basedOn/joinKeys", new[] { "Broker" })
            .WithProperty("basedOn/openProp", "Open")
            .WithProperty("basedOn/closeProp", "KsqlTimeFrameClose")
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
    public void Core_Builds_Final_Window_EmitFinal_SyncsOnlyOn1m()
    {
        var md1 = BaseMd()
            .WithProperty("input/1mFinal", "src1")
            .WithProperty("sync/1mFinal", "HB_1m")
            .WithProperty("prev/1mFinal", "bar_prev_1m");
        var q1 = FinalBuilder.Build(md1, "1m");
        Assert.StartsWith("TABLE src1", q1);
        Assert.Contains("WINDOW TUMBLING(1m)", q1);
        Assert.Contains("EMIT FINAL", q1);
        Assert.Contains("SYNC HB_1m", q1);
        Assert.Contains("PREV bar_prev_1m", q1);
        Assert.DoesNotContain("COMPOSE(", q1);

        var md5 = BaseMd().WithProperty("input/5mFinal", "src5");
        var q5 = FinalBuilder.Build(md5, "5m");
        Assert.StartsWith("TABLE src5", q5);
        Assert.Contains("WINDOW TUMBLING(5m)", q5);
        Assert.DoesNotContain("SYNC", q5);
        Assert.DoesNotContain("COMPOSE(", q5);
    }

    [Fact]
    public void FinalBuilder_Uses_PerTimeframe_Grace()
    {
        var md = BaseMd()
            .WithProperty("input/1mFinal", "src")
            .WithProperty("grace/1m", 4);
        var q = FinalBuilder.Build(md, "1m");
        Assert.Contains("WINDOW TUMBLING(1m GRACE PERIOD 4s)", q);
    }

    [Fact]
    public void Core_Applies_TimeFrame_Join_And_Boundary_To_Live_And_Final()
    {
        var md = BaseMd().WithProperty("input/1mLive", "s");
        var live = LiveBuilder.Build(md, "1m");
        var fin = FinalBuilder.Build(md.WithProperty("input/1mFinal", "a"), "1m");
        foreach (var q in new[] { live, fin })
            Assert.Contains("JOIN ON", q);
    }

    [Fact]
    public void Builders_Expand_TimeFrame_Join_And_Boundary_For_Live_And_Final()
    {
        var md = BaseMd().WithProperty("input/1mLive", "s");
        var live = LiveBuilder.Build(md, "1m");
        var fin = FinalBuilder.Build(md.WithProperty("input/1mFinal", "a"), "1m");
        Assert.Contains("s.Open <= r.Ts", live);
        Assert.Contains("s.Open <= r.Ts", fin);
    }
}
