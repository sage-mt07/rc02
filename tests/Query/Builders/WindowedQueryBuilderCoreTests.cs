using Kafka.Ksql.Linq.Core.Attributes;
using Kafka.Ksql.Linq.Query.Builders;
using Kafka.Ksql.Linq.Query.Pipeline;
using System;
using Xunit;

namespace Kafka.Ksql.Linq.Tests.Query.Builders;

public class WindowedQueryBuilderCoreTests
{
    [KsqlTopic("bar")]
    private class Source { }

    private static ExpressionAnalysisResult BaseRes(string tf)
    {
        var res = new ExpressionAnalysisResult
        {
            PocoType = typeof(Source),
            TimeKey = "Ts",
            BasedOnOpen = "Open",
            BasedOnClose = "KsqlTimeFrameClose",
            BasedOnDayKey = "MarketDate"
        };
        res.BasedOnJoinKeys.Add("Broker");
        res.Windows.Add(tf);
        return res;
    }

    private static QueryMetadata Md(string tf) => BaseRes(tf).ToMetadata();

    [Fact]
    public void Core_Builds_Live_Table_EmitChanges_NoSync()
    {
        var q1 = LiveBuilder.Build(Md("1m"), "1m");
        Assert.StartsWith("TABLE bar_1s_final_s", q1);
        Assert.DoesNotContain("SYNC", q1);

        var q5 = LiveBuilder.Build(Md("5m"), "5m");
        Assert.DoesNotContain("SYNC", q5);
    }

    [Fact]
    public void Core_Builds_Final_Window_EmitFinal_NoSync()
    {
        var q1 = FinalBuilder.Build(Md("1m"), "1m");
        Assert.StartsWith("TABLE bar_1s_final_s", q1);
        Assert.Contains("WINDOW TUMBLING(1m)", q1);
        Assert.Contains("EMIT FINAL", q1);
        Assert.DoesNotContain("SYNC", q1);
        Assert.DoesNotContain("COMPOSE(", q1);

        var q5 = FinalBuilder.Build(Md("5m"), "5m");
        Assert.StartsWith("TABLE bar_1s_final_s", q5);
        Assert.Contains("WINDOW TUMBLING(5m)", q5);
        Assert.DoesNotContain("SYNC", q5);
        Assert.DoesNotContain("COMPOSE(", q5);
    }

    [Fact]
    public void FinalBuilder_Uses_PerTimeframe_Grace()
    {
        var res = BaseRes("1m");
        res.GracePerTimeframe["1m"] = 4;
        var q = FinalBuilder.Build(res.ToMetadata(), "1m");
        Assert.Contains("WINDOW TUMBLING(1m GRACE PERIOD 4s)", q);
    }

    [Fact]
    public void Core_Applies_TimeFrame_Join_And_Boundary_To_Live_And_Final()
    {
        var md = Md("1m");
        var live = LiveBuilder.Build(md, "1m");
        var fin = FinalBuilder.Build(md, "1m");
        foreach (var q in new[] { live, fin })
            Assert.Contains("JOIN ON", q);
    }

    [Fact]
    public void Builders_Expand_TimeFrame_Join_And_Boundary_For_Live_And_Final()
    {
        var md = Md("1m");
        var live = LiveBuilder.Build(md, "1m");
        var fin = FinalBuilder.Build(md, "1m");
        Assert.Contains("s.Open <= r.Ts", live);
        Assert.Contains("s.Open <= r.Ts", fin);
    }
}

